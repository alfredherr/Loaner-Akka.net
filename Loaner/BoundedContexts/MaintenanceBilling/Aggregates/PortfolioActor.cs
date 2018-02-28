using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Akka.Actor;
using Akka.Dispatch;
using Akka.Event;
using Akka.Monitoring;
using Akka.Persistence;
using Loaner.ActorManagement;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;
using Loaner.KafkaProducer.Commands;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    using static LoanerActors;

    public class PortfolioActor : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();


        private PorfolioState _porfolioState = new PorfolioState();

        protected PropertyInfo NumberOfMessagesProperty =
            typeof(Mailbox).GetProperty("NumberOfMessages", BindingFlags.Instance | BindingFlags.NonPublic);


        public PortfolioActor()
        {
            /*** recovery section **/
            Recover<SnapshotOffer>(offer => ProcessSnapshot(offer));
            Recover<AccountAddedToSupervision>(command => ReplayEvent(command.AccountNumber));

            /** Core Commands **/
            Command<SuperviseThisAccount>(command => ProcessSupervision(command));
            Command<StartAccounts>(command => StartAccounts());
            Command<AssessWholePortfolio>(cmd => AssessAllAccounts(cmd));
            Command<CheckYoSelf>(cmd => RegisterStartup());
            /* Common comands */
            Command<TellMeYourStatus>(asking => GetMyStatus());
            Command<TellMeAboutYou>(me =>
                Console.WriteLine(
                    $"About me: I am {Self.Path.Name} Msg: {me.Me} I was last booted up on: {_porfolioState.LastBootedOn}"));
            Command<TellMeYourPortfolioStatus>(msg => _log.Debug(msg.Message));
            Command<string>(noMessage => { });
            Command<RegisterMyAccountBalanceChange>(cmd => RegisterBalanceChange(cmd));

            Command<PublishPortfolioStateToKafka>(cmd => PublishToKafka(cmd));
            Command<ReportDebugInfo>(cmd => ReportDebugInfo(cmd));


            /** Special handlers below; we can decide how to handle snapshot processin outcomes. */
            Command<SaveSnapshotSuccess>(success => PurgeOldSnapShots(success));
            Command<DeleteSnapshotsSuccess>(msg => { });
            Command<SaveSnapshotFailure>(
                failure => _log.Error(
                    $"Actor {Self.Path.Name} was unable to save a snapshot. {failure.Cause.Message}"));
            Command<DeleteMessagesSuccess>(
                msg => _log.Debug($"Successfully cleared log after snapshot ({msg.ToString()})"));
            Command<MyAccountStatus>(msg =>
                _log.Debug(
                    $"Why is account {msg.AccountState.AccountNumber} sending me an 'MyAccountStatus' message?"));
            CommandAny(msg => _log.Error($"Unhandled message in {Self.Path.Name}. Message:{msg.ToString()}"));
        }

        public override string PersistenceId => Self.Path.Name;

        protected void ReportMailboxSize()
        {
            var context = Context as ActorCell;
            if (context == null)
                return;

            var mailbox = context.Mailbox;
            var numberOfMessages = (int) NumberOfMessagesProperty.GetValue(mailbox);

            Context.Gauge("PortfolioMailbox", numberOfMessages);
        }

        private void ReportDebugInfo(ReportDebugInfo msg)
        {
            var active = _porfolioState.SupervizedAccounts.Count(x => x.AccountActorRef != null);

            var totalBillings =
                _porfolioState.SupervizedAccounts.Aggregate(0.0, (x, y) => x + y.BalanceAfterLastTransaction);

            _log.Info(
                $"DebugInfo: {Self.Path.Name} has {_porfolioState.SupervizedAccounts.Count} " +
                $"accounts under supervision " +
                $"of which, {active} are active " +
                $"with a total balance of ${totalBillings}" +
                $" (report#{_porfolioState.ScheduledCallsToInfo++})");
        }

        private void PublishToKafka(PublishPortfolioStateToKafka cmd)
        {
            var portfolioSate = new PortfolioStateViewModel
            {
                AccountCount = _porfolioState.SupervizedAccounts.Count,
                AsOfDate = DateTime.Now,
                PortfolioName = Self.Path.Name
            };
            var totalBillings =
                _porfolioState.SupervizedAccounts.Aggregate(0.0, (x, y) => x + y.BalanceAfterLastTransaction);

            portfolioSate.TotalBalance = (decimal) totalBillings;

            var key = portfolioSate.PortfolioName;
            PortfolioStatePublisherActor.Tell(new Publish(key, portfolioSate));
            _log.Debug($"Sending kafka message for portfolio {key}");
        }

        private void PurgeOldSnapShots(SaveSnapshotSuccess success)
        {
            var snapshotSeqNr = success.Metadata.SequenceNr;
            // delete all messages from journal and snapshot store before latests confirmed
            // snapshot, we won't need them anymore
            DeleteMessages(snapshotSeqNr);
            DeleteSnapshots(new SnapshotSelectionCriteria(snapshotSeqNr - 1));
        }

        private void RegisterBalanceChange(RegisterMyAccountBalanceChange cmd)
        {
            var account = _porfolioState.SupervizedAccounts.FirstOrDefault(x => x.AccountNumber == cmd.AccountNumber);

            if (account == null)
                account = new AccountUnderSupervision(cmd.AccountNumber);

            account.LastTransactionAmount = cmd.AmountTransacted;
            account.BalanceAfterLastTransaction = cmd.AccountBalanceAfterTransaction;


            var billed = _porfolioState.SupervizedAccounts.Count(x => x.LastTransactionAmount > 0.0);

            if (billed % 10000 == 0)
            {
                var viewble = new Dictionary<string, Tuple<double, double>>();
                foreach (var a in _porfolioState.SupervizedAccounts)
                    viewble.Add(a.AccountNumber, Tuple.Create(a.LastTransactionAmount, a.BalanceAfterLastTransaction));
                Context.Parent.Tell(new RegisterPortolioBilling(Self.Path.Name, viewble));
                _log.Debug(
                    $"{Self.Path.Name} sent {Context.Parent.Path.Name} portfolio billing message containing {viewble.Count} billed accounts ");
            }


            Self.Tell(new PublishPortfolioStateToKafka());
        }

        private void RegisterStartup()
        {
            _porfolioState.LastBootedOn = DateTime.Now;
        }

        private void AssessAllAccounts(AssessWholePortfolio cmd)
        {
            Monitor();
            foreach (var account in _porfolioState.SupervizedAccounts)
            {
                var bill = new BillingAssessment(account.AccountNumber, cmd.Items);
                account.AccountActorRef.Tell(bill);
            }

            Sender.Tell(new TellMeYourPortfolioStatus(
                $"Your request was sent to all {_porfolioState.SupervizedAccounts.Count} accounts",
                null));
        }


        protected override void PostStop()
        {
            Context.IncrementActorStopped();
        }

        protected override void PreStart()
        {
            Context.IncrementActorCreated();

            DemoActorSystem.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(10), Self, new ReportDebugInfo(), ActorRefs.NoSender);
        }

        private void Monitor()
        {
            Context.IncrementMessagesReceived();
            //ReportMailboxSize();
        }

        private void RecoveryCounter()
        {
            Context.IncrementCounter("PortfolioRecovery");
        }


        private Dictionary<string, string> DictionaryToStringList()
        {
            var viewble = new Dictionary<string, string>();
            foreach (var a in _porfolioState.SupervizedAccounts)
                viewble.Add(a.AccountNumber, a.AccountActorRef?.ToString() ?? "Not Instantiated");
            return viewble;
        }

        private void StartAccounts()
        {
            Monitor();

            var immutAccounts = _porfolioState.SupervizedAccounts.ToImmutableList();

            foreach (var account in immutAccounts)
                if (account.AccountActorRef == null)
                    account.AccountActorRef = InstantiateThisAccount(account);
                else
                    _log.Warning($"skipped account {account}, already instantiated.");
            Sender.Tell(
                new TellMeYourPortfolioStatus(
                    $"{_porfolioState.SupervizedAccounts.Count} accounts. I was last booted up on: {_porfolioState.LastBootedOn}",
                    null));
        }

        private void GetMyStatus()
        {
            var portfolioSate = new PortfolioStateViewModel
            {
                AccountCount = _porfolioState.SupervizedAccounts.Count,
                AsOfDate = DateTime.Now,
                PortfolioName = Self.Path.Name
            };

            var totalBillings =
                _porfolioState.SupervizedAccounts.Aggregate(0.0, (x, y) => x + y.BalanceAfterLastTransaction);

            portfolioSate.TotalBalance = (decimal) totalBillings;

            var key = portfolioSate.PortfolioName;


            Sender.Tell(new TellMeYourPortfolioStatus(
                $"{_porfolioState.SupervizedAccounts.Count} accounts. I was last booted up on: {_porfolioState.LastBootedOn.ToString("yyyy-MM-dd h:mm tt")}",
                portfolioSate));
        }

        private void ProcessSupervision(SuperviseThisAccount command)
        {
            Monitor();

            var account = new AccountUnderSupervision(command.AccountNumber);
            var @event = new AccountAddedToSupervision(command.AccountNumber);
            Persist(@event, s =>
            {
                account.AccountActorRef = InstantiateThisAccount(account);
                _porfolioState.SupervizedAccounts.Add(account);
                ApplySnapShotStrategy();
                Self.Tell(new PublishPortfolioStateToKafka());
            });
        }

        private void ReplayEvent(string accountNumber)
        {
            RecoveryCounter();
            if (string.IsNullOrEmpty(accountNumber)) throw new Exception("Why is this blank?");

            if (AccountExistInState(accountNumber))
            {
                _log.Debug($"Supervisor already has {accountNumber} in state. No action taken");
                return;
            }

            var account = new AccountUnderSupervision(accountNumber);
            account.AccountActorRef = InstantiateThisAccount(account);
            _porfolioState.SupervizedAccounts.Add(account);

            // TODO probably will have to ask the account for some more data, like curr bal, etc.

            _log.Debug($"Replayed event on {accountNumber}");
        }

        private bool AccountExistInState(string accountNumber)
        {
            return _porfolioState.SupervizedAccounts.Select(x => x.AccountNumber == accountNumber).Any();
        }

        private IActorRef InstantiateThisAccount(AccountUnderSupervision account)
        {
            var accountActor = Context.ActorOf(Props.Create<AccountActor>(), account.AccountNumber);

            account.AccountActorRef = accountActor; // is this neede?

            _log.Debug($"Instantiated account {accountActor.Path.Name}");

            return accountActor;
        }

        private void ProcessSnapshot(SnapshotOffer offer)
        {
            Monitor();

            _porfolioState = (PorfolioState) offer.Snapshot;

            //Clear out the old address reference
            _porfolioState.SupervizedAccounts.ForEach(x => x.AccountActorRef = null);

            _log.Info($"{Self.Path.Name} Snapshot recovered. {_porfolioState.SupervizedAccounts.Count} accounts.");
        }

        public void ApplySnapShotStrategy()
        {
            if (LastSequenceNr % TakePortolioSnapshotAt == 0 || PersistenceId.ToUpper().Contains("PORTFOLIO"))
            {
                SaveSnapshot(_porfolioState);
                _log.Info($"Portfolio {Self.Path.Name} snapshot taken. Current SequenceNr is {LastSequenceNr}.");
                Context.IncrementCounter("SnapShotTaken");
            }
        }
    }
}
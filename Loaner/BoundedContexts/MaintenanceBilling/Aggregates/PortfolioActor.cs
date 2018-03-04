using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Akka.Actor;
using Akka.Dispatch;
using Akka.Event;
using Akka.Monitoring;
using Akka.Persistence;
using Akka.Routing;
using Akka.Util.Internal;
using Loaner.ActorManagement;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;
using Loaner.KafkaProducer.Commands;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    using static LoanerActors;

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class PortfolioActor : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();


        private PorfolioState _porfolioState = new PorfolioState();

        private readonly PropertyInfo NumberOfMessagesProperty =
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
            Command<ReportPortfolioStateToParent>(cmd => ReportPortfolioStateToParent());
            Command<ReportMailboxSize>(cmd => ReportMailboxSize());
            
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
      
            _log.Info($"[ReportMailboxSize]: {PersistenceId} Mailbox Size: {GetMailboxSize() :##,#}");
        }

        private int GetMailboxSize()
        {
            if (!(Context is ActorCell context))
                return 0;

            var mailbox = context.Mailbox;
            var numberOfMessages = (int) NumberOfMessagesProperty.GetValue(mailbox);

            return numberOfMessages;
        }
        
        private void ReportPortfolioStateToParent()
        {
            var viewble = new Dictionary<string, Tuple<double, double>>();
            foreach (var a in _porfolioState.SupervizedAccounts?.Values.ToList())
                viewble.Add(a.AccountNumber, Tuple.Create(a.LastBilledAmount, a.BalanceAfterLastTransaction));
            Context.Parent.Tell(new RegisterPortolioBilling(Self.Path.Name, viewble));
            double totalBal = viewble.Aggregate(0.0, (x, y) => x + y.Value.Item2);
           
            _log.Debug($"[ReportPortfolioStateToParent]: {Self.Path.Name} sent {Context.Parent.Path.Name} portfolio" +
                      $" billing message containing {viewble.Count :##,#} billed accounts and a balance of {totalBal :C} ");

        }
        private void ReportDebugInfo(ReportDebugInfo msg)
        {
            var active = _porfolioState.SupervizedAccounts.Count(x => x.Value.AccountActorRef != null);

            var totalBillings =
                _porfolioState.SupervizedAccounts.Aggregate(0.0, (x, y) => x + y.Value.BalanceAfterLastTransaction);

            _log.Info(
                $"[ReportDebugInfo]: {Self.Path.Name} has {_porfolioState.SupervizedAccounts.Count :##,#} " +
                $"accounts under supervision, " +
                $"of which, {active :##,#} are active " +
                $"with a total balance of {totalBillings :C}" +
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
                _porfolioState.SupervizedAccounts.Aggregate(0.0, (x, y) => x + y.Value.BalanceAfterLastTransaction);

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
            AccountUnderSupervision account = _porfolioState.SupervizedAccounts.FirstOrDefault(x => x.Key == cmd.AccountNumber).Value;

            if(account == null)
            {
                account = new AccountUnderSupervision(cmd.AccountNumber);

            }
            _log.Debug($"[RegisterBalanceChange]: account.BalanceAfterLastTransaction={account.BalanceAfterLastTransaction}\t" +
                      $"cmd.AccountBalanceAfterTransaction={cmd.AccountBalanceAfterTransaction}\n" +
                      $"account.LastBilledAmount={account.LastBilledAmount}\tcmd.AmountTransacted={cmd.AmountTransacted}");

            account.LastBilledAmount = cmd.AmountTransacted;
            account.BalanceAfterLastTransaction = cmd.AccountBalanceAfterTransaction;

            _porfolioState.SupervizedAccounts.AddOrSet(cmd.AccountNumber, account);
            _porfolioState.UpdateBalance();
            
            ApplySnapShotStrategy();
            Self.Tell(new PublishPortfolioStateToKafka());
            
        }

        private void RegisterStartup()
        {
            _porfolioState.LastBootedOn = DateTime.Now;
        }

        private void AssessAllAccounts(AssessWholePortfolio cmd)
        {
            Monitor();

            Console.WriteLine($"Billing Items: {cmd.Items.Aggregate("",(acc, next) => acc +" "+ next.Item.Name + " " + next.Item.Amount)}");
            foreach (var account in _porfolioState.SupervizedAccounts?.Values.ToList())
            {
                var bill = new BillingAssessment(account.AccountNumber, cmd.Items, AccountBusinessRulesRouter);
                account.AccountActorRef.Tell(bill);
                //_log.Info($"[AssessAllAccounts]: Just told account {account.AccountNumber} to run assessment.");
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
                TimeSpan.FromSeconds(30), Self, new ReportDebugInfo(), ActorRefs.NoSender);

            DemoActorSystem.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(10), Self, new ReportPortfolioStateToParent(), ActorRefs.NoSender);
//
//            DemoActorSystem.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0),
//                TimeSpan.FromSeconds(10), Self, new ReportMailboxSize(), ActorRefs.NoSender);
        }

        private void Monitor()
        {
            Context.IncrementMessagesReceived();            
        }

        private void RecoveryCounter()
        {
            Context.IncrementCounter("PortfolioRecovery");
        }


        private Dictionary<string, string> DictionaryToStringList()
        {
            var viewble = new Dictionary<string, string>();
            foreach (var a in _porfolioState.SupervizedAccounts?.Values.ToList())
                viewble.Add(a.AccountNumber, a.AccountActorRef?.ToString() ?? "Not Instantiated");
            return viewble;
        }

        private void StartAccounts()
        {
            Monitor();

            var immutAccounts = _porfolioState.SupervizedAccounts.Values.ToImmutableList();

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
                PortfolioName = Self.Path.Name, 
                TotalBalance =  _porfolioState.CurrentPortfolioBalance
            };
            
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
                _porfolioState.SupervizedAccounts.AddOrSet(command.AccountNumber,account);
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
            _porfolioState.SupervizedAccounts.AddOrSet(accountNumber,account);

            // TODO probably will have to ask the account for some more data, like curr bal, etc.

            _log.Debug($"Replayed event on {accountNumber}");
        }

        private bool AccountExistInState(string accountNumber)
        {
            return _porfolioState.SupervizedAccounts.Select(x => x.Value.AccountNumber == accountNumber).Any();
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
            _porfolioState.SupervizedAccounts.ForEach(x => x.Value.AccountActorRef = null);

            //clear out the billed amount
            _porfolioState.SupervizedAccounts.ForEach(x => x.Value.LastBilledAmount = 0.0);
            
            _log.Info($"{Self.Path.Name} Snapshot recovered. {_porfolioState.SupervizedAccounts.Count} accounts.");
        }

        public void ApplySnapShotStrategy()
        {
            if (LastSequenceNr % TakePortolioSnapshotAt == 0 || PersistenceId.ToUpper().Contains("PORTFOLIO"))
            {
                var clonedState = _porfolioState.Clone();
                SaveSnapshot(clonedState);
                _log.Info($"[ApplySnapShotStrategy]: Portfolio {Self.Path.Name} snapshot taken. Current SequenceNr is {LastSequenceNr}.");
                Context.IncrementCounter("SnapShotTaken");
            }
        }
    }
}
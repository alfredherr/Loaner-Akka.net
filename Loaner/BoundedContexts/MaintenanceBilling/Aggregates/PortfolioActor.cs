

using System.Reflection;
using Akka.Dispatch;
using Loaner.KafkaProducer;
using Loaner.KafkaProducer.Commands;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using Akka.Actor;
    using Akka.Event;
    using Akka.Monitoring;
    using Akka.Persistence;
    using Akka.Util.Internal;
    using ActorManagement;
    using Messages;
    using Models;
    using DomainCommands;
    using DomainEvents;
    using static ActorManagement.LoanerActors;

    public class PortfolioActor : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        /**
         * Actor's state = just a list of account under supervision
         */
        private Dictionary<string, IActorRef> _accounts = new Dictionary<string, IActorRef>();

        private Dictionary<string, Tuple<double, double>> _billings = new Dictionary<string, Tuple<double, double>>();
        private int _scheduledCallsToInfo = 0;
        private DateTime _lastBootedOn;

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
                    $"About me: I am {Self.Path.Name} Msg: {me.Me} I was last booted up on: {_lastBootedOn}"));
            Command<TellMeYourPortfolioStatus>(msg => _log.Debug(msg.Message));
            Command<string>(noMessage => { });
            Command<RegisterMyAccountBilling>(cmd => RegisterBillingStatus(cmd));

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
        protected PropertyInfo NumberOfMessagesProperty = typeof(Mailbox).GetProperty("NumberOfMessages", BindingFlags.Instance | BindingFlags.NonPublic);

        protected void ReportMailboxSize()
        {
            
            var context = Context as ActorCell;
            if (context == null)
                return;

            var mailbox = context.Mailbox;
            var numberOfMessages = (int)NumberOfMessagesProperty.GetValue(mailbox);

            Context.Gauge("PortfolioMailbox", numberOfMessages);
        }
        private void ReportDebugInfo(ReportDebugInfo msg)
        {
            var active = _accounts.Aggregate(0, (x, y) => x + ( (y.Value == null || y.Value.Equals(ActorRefs.Nobody)) ? 0 : 1) );
            _log.Info(
                $"DebugInfo: {Self.Path.Name} has {_accounts.Count} accounts under supervision " + 
                $"of which, {active} are active " +
                $"with a total balance of ${_billings.Aggregate(0.0, (x, y) => y.Value.Item2 + x)}" +
                $" (report#{_scheduledCallsToInfo++})");

        }
        private void PublishToKafka(PublishPortfolioStateToKafka cmd)
        {
            var portfolioSate = new PortfolioState
            {
                AccountCount = _accounts.Count,
                AsOfDate = DateTime.Now,
                Name = Self.Path.Name
            };
            double total = 0.00;
            portfolioSate.TotalBalance = (decimal)_billings.Aggregate(total, ( x, y ) =>  y.Value.Item2  + x);
            portfolioSate.ID = GetPorfolioNameHash(Self.Path.Name);

            var key = portfolioSate.Name;
            PortfolioStatePublisherActor.Tell(new Publish(key, portfolioSate));
            _log.Debug($"Sending kafka message for portfolio {key}");

        }

        public static long GetPorfolioNameHash(string portfolioName)
        {
            string input = portfolioName;
            var s1 = input.Substring(0, input.Length / 2);
            var s2 = input.Substring(input.Length / 2);

            var x = ((long)s1.GetHashCode()) << 0x20 | s2.GetHashCode();
            return x;
            
        }

        private void PurgeOldSnapShots(SaveSnapshotSuccess success)
        {
            var snapshotSeqNr = success.Metadata.SequenceNr;
            // delete all messages from journal and snapshot store before latests confirmed
            // snapshot, we won't need them anymore
            DeleteMessages(snapshotSeqNr);
            DeleteSnapshots(new SnapshotSelectionCriteria(snapshotSeqNr - 1));
        }

        private void RegisterBillingStatus(RegisterMyAccountBilling cmd)
        {
            _billings.AddOrSet(cmd.AccountNumber, Tuple.Create(cmd.AmountBilled, cmd.AccountBalanceAfterBilling));

            var viewble = new Dictionary<string, Tuple<double, double>>();
            foreach (var a in _billings)
            {
                viewble.Add(a.Key, a.Value);
            }

            Context.Parent.Tell(new RegisterPortolioBilling(Self.Path.Name, viewble));
            _log.Debug(
                $"{Self.Path.Name} sent {Context.Parent.Path.Name} portfolio billing message containing {viewble.Count} billed accounts ");

            Self.Tell(new PublishPortfolioStateToKafka());
        }

        private void RegisterStartup()
        {
            _lastBootedOn = DateTime.Now;
           
        }

        private void AssessAllAccounts(AssessWholePortfolio cmd)
        {
            Monitor();
            foreach (var account in _accounts)
            {
                BillingAssessment bill = new BillingAssessment(account.Key, cmd.Items);
                account.Value.Tell(bill);
            }
            Sender.Tell(new TellMeYourPortfolioStatus($"Your request was sent to all {_accounts.Count} accounts",
                DictionaryToStringList()));
        }

        public override string PersistenceId => Self.Path.Name;


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
            foreach (var a in _accounts)
                viewble.Add(a.Key, a.Value?.ToString() ?? "Not Instantiated");
            return viewble;
        }

        private void StartAccounts()
        {
            Monitor();
            var immutAccounts = _accounts.Keys.ToList();

            foreach (var account in immutAccounts)
                if (account.Length != 0 && _accounts[account] == null)
                {
                    InstantiateThisAccount(account);
                }
                else
                {
                    _log.Warning($"skipped account {account}, already instantiated.");
                }
            Sender.Tell(
                new TellMeYourPortfolioStatus($"{_accounts.Count} accounts. I was last booted up on: {_lastBootedOn}",
                    null));
        }

        private void GetMyStatus()
        {
            var tooMany = new Dictionary<string, string>();
            tooMany.Add("sorry", "Too many accounts to list here");
            Sender.Tell(new TellMeYourPortfolioStatus(
                $"{_accounts.Count} accounts. I was last booted up on: {_lastBootedOn}",
                (_accounts.Count > 300_000) ? tooMany : DictionaryToStringList()));
        }

        private void ProcessSupervision(SuperviseThisAccount command)
        {
            Monitor();
            if (!_accounts.ContainsKey(command.AccountNumber))
            {
                var @event = new AccountAddedToSupervision(command.AccountNumber);
                Persist(@event, s =>
                {
                    _accounts.Add(command.AccountNumber, null);
                    InstantiateThisAccount(command.AccountNumber);
                    ApplySnapShotStrategy();
                    Self.Tell(new PublishPortfolioStateToKafka());
                });
                
            }
            else
            {
                _log.Info($"You tried to load account {command.AccountNumber} which has already been loaded");
            }
            
        }

        private void ReplayEvent(string accountNumber)
        {
            RecoveryCounter();
            if (string.IsNullOrEmpty(accountNumber))
            {
                throw new Exception("Why is this blank?");
            }

            if (_accounts.ContainsKey(accountNumber))
            {
                _log.Debug($"Supervisor already has {accountNumber} in state. No action taken");
            }
            else
            {
                _accounts.Add(accountNumber, null);
                _log.Debug($"Replayed event on {accountNumber}");
            }
        }

        private IActorRef InstantiateThisAccount(string accountNumber)
        {
            if (_accounts.ContainsKey(accountNumber))
            {
                var accountActor = Context.ActorOf(Props.Create<AccountActor>(), accountNumber);
                _accounts[accountNumber] = accountActor;
                //accountActor.Tell(new CheckYoSelf()); // to instantiate actor
                _log.Debug($"Instantiated account {accountActor.Path.Name}");
                return accountActor;
            }
            throw new Exception($"Why are you trying to instantiate an account not yet registered?");
        }

        private void ProcessSnapshot(SnapshotOffer offer)
        {
            Monitor();

            //var snap = ((Newtonsoft.Json.Linq.JArray) offer.Snapshot).ToObject<string[]>();
            var snap = (string[]) offer.Snapshot;

            foreach (var account in snap)
            {
                _accounts.Add(account, null);
            }
            _log.Info($"{Self.Path.Name} Snapshot recovered. {snap.Length} accounts.");
        }

        public void ApplySnapShotStrategy()
        {
            if (LastSequenceNr % LoanerActors.TakePortolioSnapshotAt == 0)
            {
                var state = new List<string>(); // Just need the name to kick it off?
                foreach (var record in _accounts.Keys)
                {
                    state.Add(record);
                }
                SaveSnapshot(state.ToArray());
                _log.Info($"Portfolio {Self.Path.Name} snapshot taken. Current SequenceNr is {LastSequenceNr}.");
                Context.IncrementCounter("SnapShotTaken");
            }
        }
    }
}
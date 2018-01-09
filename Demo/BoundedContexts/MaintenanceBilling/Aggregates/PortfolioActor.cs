using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using Akka.Persistence;
using Akka.Util.Internal;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Demo.BoundedContexts.MaintenanceBilling.Commands;
using Demo.BoundedContexts.MaintenanceBilling.Events;
//using Demo.Custom.Persistence;
using static Demo.ActorManagement.LoanerActors;

namespace Demo.BoundedContexts.MaintenanceBilling.Aggregates
{
    public class PortfolioActor : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        //private OneDatabasePerActor db;

        /**
         * Actor's state = just a list of account under supervision
         */
        private Dictionary<string, IActorRef> _accounts = new Dictionary<string, IActorRef>();
        private Dictionary<string,Tuple<double,double>> _billings = new Dictionary<string, Tuple<double,double>>();
        
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
            Command<AboutMe>(me => Console.WriteLine($"About me: {me.Me}. I was last booted up on: {_lastBootedOn}"));
            Command<MyPortfolioStatus>(msg => _log.Debug(msg.Message));
            Command<string>(noMessage => { });
            Command<RegisterMyAccountBilling>(cmd => RegisterBillingStatus(cmd));
            
            /** Special handlers below; we can decide how to handle snapshot processin outcomes. */
            Command<SaveSnapshotSuccess>(success => PurgeOldSnapShots(success));
            Command<DeleteSnapshotsSuccess>(msg => { });
            Command<SaveSnapshotFailure>(
                failure => _log.Error(
                    $"Actor {Self.Path.Name} was unable to save a snapshot. {failure.Cause.Message}"));
            Command<DeleteMessagesSuccess>(
                msg => _log.Debug($"Successfully cleared log after snapshot ({msg.ToString()})"));
            CommandAny(msg => _log.Error($"Unhandled message in {Self.Path.Name}. Message:{msg.ToString()}"));
        }
        private void PurgeOldSnapShots(SaveSnapshotSuccess success)
        {
            var snapshotSeqNr = success.Metadata.SequenceNr;
            // delete all messages from journal and snapshot store before latests confirmed
            // snapshot, we won't need them anymore
            //DeleteMessages(snapshotSeqNr);
            //DeleteSnapshots(new SnapshotSelectionCriteria(snapshotSeqNr - 1));
        }
        private void RegisterBillingStatus(RegisterMyAccountBilling cmd)
        {
           _billings.AddOrSet(cmd.AccountNumber,Tuple.Create(cmd.AmountBilled, cmd.AccountBalanceAfterBilling));
            
            var viewble = new Dictionary<string, Tuple<double,double>>();
            foreach (var a in _billings)
            {
                viewble.Add(a.Key,a.Value);
            }
 
            Context.Parent.Tell(new RegisterPortolioBilling(Self.Path.Name ,viewble) ) ;
            _log.Info($"{Self.Path.Name} sent {Context.Parent.Path.Name} portfolio billing message containting {viewble.Count} billed accounts ");
          
        }

        private void RegisterStartup()
        {
            _lastBootedOn = DateTime.Now;
        }

        private void AssessAllAccounts(AssessWholePortfolio cmd)
        {
            foreach (var account in _accounts)
            {
                BillingAssessment bill = new BillingAssessment(account.Key,cmd.Items) ;
                account.Value.Tell(bill);   
            }
            Sender.Tell(new MyPortfolioStatus($"Your request was sent to all { _accounts.Count } accounts",DictionaryToStringList()));
        }

        public override string PersistenceId => Self.Path.Name;

        protected override void PostStop()
        {
            Context.IncrementActorStopped();
        }

        protected override void PreStart()
        {
            Context.IncrementActorCreated();
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
            foreach (var a in _accounts)
                viewble.Add(a.Key, a.Value?.ToString() ?? "Not Instantiated");
            return viewble;
        }

        private void StartAccounts()
        {
            Monitor();
            var immutAccounts = _accounts.Keys.ToList();
            
            foreach (var account in immutAccounts )
                if (account.Length != 0 && _accounts[account] == null)
                {
                    InstantiateThisAccount(account);
                }
                else
                {
                    _log.Warning($"skipped account {account}, already instantiated.");
                }
            Sender.Tell(new MyPortfolioStatus($"{_accounts.Count} accounts. I was last booted up on: {_lastBootedOn}",null));
        }

        private void GetMyStatus()
        {
            var tooMany = new Dictionary<string, string>();
            tooMany.Add("sorry","Too many accounts to list here");
            Sender.Tell(new MyPortfolioStatus($"{_accounts.Count} accounts. I was last booted up on: {_lastBootedOn}",
                (_accounts.Count > 10000) ? tooMany : DictionaryToStringList()));
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
                    ApplySnapShotStrategy();
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
                accountActor.Tell(new CheckYoSelf()); // to instantiate actor
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
            _log.Info($"Snapshot recovered. ${snap} accounts.");
        }
        //private void CustomPersistence()
        //{
        //    if (db == null)
        //    {
        //        db = new OneDatabasePerActor(PersistenceId);
        //    }
        //    var state = new List<string>(); // Just need the name to kick it off?
        //    foreach (var record in _accounts.Keys)
        //        state.Add(record);
        //    SaveSnapshot(state.ToArray());
        //    db.Snapshot(PersistenceId, typeof(PortfolioActor).ToString(), state.ToArray());
        //}
        public void ApplySnapShotStrategy()
        {
            if (LastSequenceNr != 0 && LastSequenceNr % TAKE_PORTFOLIO_SNAPSHOT_AT == 0)
            {
                Context.IncrementCounter("SnapShotTaken");
                //CustomPersistence();
                var state = new List<string>();
                foreach (var record in _accounts.Keys)
                {
                    state.Add(record);
                }
                SaveSnapshot(state.ToArray());
            }
        }
    }

    
}
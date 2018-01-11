using Akka.Util.Internal;
using Loaner.ActorManagement;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    using Akka.Actor;
    using Akka.Event;
    using Akka.Monitoring;
    using Akka.Persistence;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    public class SystemSupervisor : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        /**
         * Actor's state = just a list of account under supervision
         */
        private Dictionary<string, IActorRef> _portfolios = new Dictionary<string, IActorRef>();
        private Dictionary<string,Dictionary<string,Tuple<double,double>>> _portfolioBillings = new Dictionary<string, Dictionary<string, Tuple<double,double>>>();
        
        public SystemSupervisor()
        {
            /*** Recovery section **/
            Recover<SnapshotOffer>(offer => ProcessSnapshot(offer));
            Recover<PortfolioAddedToSupervision>(command => ReplayEvent(command.PortfolioNumber));

            /** Core commands **/
            Command<SimulateBoardingOfAccounts>(client => RunSimulator(client));
            Command<SuperviseThisPortfolio>(command => ProcessSupervision(command));
            Command<StartPortfolios>(command => StartPortfolios());
            
            /* Commonly used commands */
            Command<TellMeYourStatus>(asking => GetMyStatus());
            Command<TellMeAboutYou>(me => Console.WriteLine($"About me: {me.Me}"));
            Command<TellMeYourPortfolioStatus>(msg => _log.Debug(msg.Message));
            Command<string>(noMessage => { });
            
            
            Command<ReportBillingProgress>(cmd => GetBillingProgress());
            Command<RegisterPortolioBilling>(cmd => RegisterPortfolioBilling(cmd) );
            
            /** Special handlers below; we can decide how to handle snapshot processin outcomes. */
            Command<SaveSnapshotSuccess>(success => PurgeOldSnapShots(success));
            Command<DeleteSnapshotsSuccess>(msg => { });
            Command<SaveSnapshotFailure>(
                failure => _log.Error(
                    $"Actor {Self.Path.Name} was unable to save a snapshot. {failure.Cause.Message}"));
            Command<DeleteMessagesSuccess>(
                msg => _log.Info($"Successfully cleared log after snapshot ({msg.ToString()})"));
            CommandAny(msg => _log.Error($"Unhandled message in {Self.Path.Name}. Message:{msg.ToString()}"));
        }
        private void PurgeOldSnapShots(SaveSnapshotSuccess success)
        {
            var snapshotSeqNr = success.Metadata.SequenceNr;
            // delete all messages from journal and snapshot store before latests confirmed
            // snapshot, we won't need them anymore
            DeleteMessages(snapshotSeqNr);
            DeleteSnapshots(new SnapshotSelectionCriteria(snapshotSeqNr - 1));
        }
        private void RegisterPortfolioBilling(RegisterPortolioBilling cmd)
        {
            _portfolioBillings.AddOrSet(cmd.PortfolioName,cmd.AccountsBilled); 
            _log.Info($"Portfolio {cmd.PortfolioName} reporting {cmd.AccountsBilled.Count} billed accounts");
        }


        private void GetBillingProgress()
        {
            Dictionary<string, Dictionary<string, Tuple<double,double>>>
                result = new Dictionary<string, Dictionary<string, Tuple<double,double>>>();
            int accountsCntr=0;
            foreach (var x in _portfolioBillings)
            {
                result.Add(x.Key,x.Value);
                x.Value.ForEach(_ => accountsCntr++);
                
            }
            Sender.Tell(new PortfolioBillingStatus(result));
            _log.Info($"ReplyWithBillingProgress to {Sender.Path.Name} with {result.Count} portfolios with a total of {accountsCntr.ToString()} billed accounts.");
      
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
            Context.IncrementCounter("SystemRecovery");
        }
        
        private void RunSimulator(SimulateBoardingOfAccounts client)
        {
            Monitor();
           
            var boardingActor = Context.ActorOf<BoardAccountActor>($"Client{client.ClientName}");
            boardingActor.Tell(client);
            _log.Info($"Started boarding of {client.ClientName} accounts at {DateTime.Now} ");
            
        }

        private Dictionary<string, string> DictionaryToStringList()
        {
            var viewble = new Dictionary<string, string>();
            foreach (var a in _portfolios)
                viewble.Add(a.Key, a.Value?.ToString() ?? "Not Instantiated");
            return viewble;
        }

        private void StartPortfolios()
        {
            Monitor();
            var immutPortfolios = _portfolios.Keys.ToList();
            foreach (var portfolio in immutPortfolios )
                if (_portfolios[portfolio] == null)
                {
                    var actor = InstantiateThisPortfolio(portfolio);
                    actor.Tell(new StartAccounts());
                }
                else
                {
                    _portfolios[portfolio].Tell(new StartAccounts());
                }
            GetMyStatus();
        }

        private void GetMyStatus()
        {
            var tooMany = new Dictionary<string, string>();
            tooMany.Add("sorry","Too many portfolios to list here");
            Sender.Tell(new MySystemStatus($"{_portfolios.Count} portfolios started.",
                (_portfolios.Count > 999) ? tooMany : DictionaryToStringList()));
        }

        private void ProcessSupervision(SuperviseThisPortfolio command)
        {
            Monitor();
            if (!_portfolios.ContainsKey(command.PortfolioName))
            {
                var @event = new PortfolioAddedToSupervision(command.PortfolioName);
                Persist(@event, s =>
                {
                    _portfolios.Add(command.PortfolioName, null); 
                    Sender.Tell(InstantiateThisPortfolio(command.PortfolioName)); 
                });
                ApplySnapShotStrategy();
            }
            else
            {
                _log.Info($"You tried to load account {command.PortfolioName} which has already been loaded");
            }
        }

        private void ReplayEvent(string portfolioNumber)
        {
            RecoveryCounter();
            if (string.IsNullOrEmpty(portfolioNumber))
            {
                 throw new Exception("Why is this blank?");
            }
           
                if (_portfolios.ContainsKey(portfolioNumber))
                {
                    _log.Debug($"Supervisor already has {portfolioNumber} in state. No action taken");
                }
                else
                {
                    _portfolios.Add(portfolioNumber, null);
                    _log.Debug($"Replayed event on {portfolioNumber}");
                }
            
        }
 
        private IActorRef InstantiateThisPortfolio(string portfolioName)
        {
            if (_portfolios.ContainsKey(portfolioName))
            {
                var portfolioActor = Context.ActorOf(Props.Create<PortfolioActor>(), portfolioName);
                _portfolios[portfolioName] = portfolioActor;
                portfolioActor.Tell(new CheckYoSelf()); // to instantiate actor
                _log.Debug($"Instantiated portfolio {portfolioActor.Path.Name}");
                return portfolioActor;
            }
            throw new Exception($"Why are you trying to instantiate a portfolio not yet registered?");
        }
        private void ProcessSnapshot(SnapshotOffer offer)
        {
            Monitor();

            //var snap = ((Newtonsoft.Json.Linq.JArray) offer.Snapshot).ToObject<string[]>();
            var snap = (string[]) offer.Snapshot;

            foreach (var account in snap)
            {
                _portfolios.Add(account, null);
            }
            _log.Info($"Snapshot recovered.");
        }
        public void ApplySnapShotStrategy()
        {
            if (LastSequenceNr != 0 && LastSequenceNr % LoanerActors.TakeSystemSupervisorSnapshotAt == 0)
            {
                var state = new List<string>(); // Just need the name to kick it off?
                foreach (var record in _portfolios.Keys)
                    state.Add(record);
                SaveSnapshot(state.ToArray());
                //_log.Debug($"Snapshot taken. LastSequenceNr is {LastSequenceNr}.");
                Context.IncrementCounter("SnapShotTaken");
                Console.WriteLine($"PortfolioActor: {DateTime.Now}\t{LastSequenceNr}\tProcessed another snapshot");
            }
        }
    }

   
     
}
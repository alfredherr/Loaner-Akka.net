using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using Akka.Persistence;
using Akka.Util.Internal;
using Loaner.ActorManagement;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class SystemSupervisor : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly Dictionary<string, Dictionary<string, Tuple<double, double>>> _portfolioBillings =
            new Dictionary<string, Dictionary<string, Tuple<double, double>>>();

        /**
         * Actor's state = just a list of account under supervision
         */
        private readonly Dictionary<string, IActorRef> _portfolios = new Dictionary<string, IActorRef>();

        private DateTime _lastBootedOn;

        public SystemSupervisor()
        {
            /*** Recovery section **/
            Recover<SnapshotOffer>(offer => ProcessSnapshot(offer));
            Recover<PortfolioAddedToSupervision>(command => ReplayEvent(command.PortfolioNumber));

            /** Core commands **/
            Command<SimulateBoardingOfAccounts>(client => RunSimulator(client));
            Command<SuperviseThisPortfolio>(command => ProcessSupervision(command));
            Command<StartPortfolios>(command => StartPortfolios());
            Command<MySystemStatus>(cmd => Console.WriteLine(cmd.Message));

            /* Commonly used commands */
            Command<TellMeYourStatus>(asking => GetMyStatus());
            Command<BootUp>(me => DoBootUp(me));
            
            Command<TellMeYourPortfolioStatus>(msg => _log.Debug("[TellMeYourPortfolioStatus]: " + msg.Message));
            Command<string>(noMessage => { });


            Command<ReportBillingProgress>(cmd => GetBillingProgress());
            Command<RegisterPortolioBilling>(cmd => RegisterPortfolioBilling(cmd));

            /** Special handlers below; we can decide how to handle snapshot processin outcomes. */
            Command<SaveSnapshotSuccess>(success => PurgeOldSnapShots(success));

            Command<DeleteSnapshotsSuccess>(msg => { });
            Command<SaveSnapshotFailure>(
                failure => _log.Error(
                    $"[SaveSnapshotFailure]: Actor {Self.Path.Name} was unable to save a snapshot. {failure.Cause.Message}"));
            Command<DeleteMessagesSuccess>(
                msg => _log.Info($"[DeleteMessagesSuccess]: Successfully cleared log after snapshot ({msg.ToString()})"));
            CommandAny(msg => _log.Error($"[CommandAny]: Unhandled message in {Self.Path.Name}. Message:{msg.ToString()}"));
        }

        private void DoBootUp(BootUp me)
        {
            _log.Info($"About me: I am {Self.Path.Name} Msg: {me} I was last booted up on: {_lastBootedOn}");
        
            Self.Tell(new StartPortfolios());
            
        }


        public override string PersistenceId => Self.Path.Name;

        private void RegisterStartup()
        {
            _lastBootedOn = DateTime.Now;
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
            _portfolioBillings.AddOrSet(cmd.PortfolioName, cmd.AccountsBilled);
            _log.Debug($"[RegisterPortfolioBilling]: Portfolio {cmd.PortfolioName} reporting {cmd.AccountsBilled.Count} billed accounts");
        }


        private void GetBillingProgress()
        {
            var result = new Dictionary<string, Dictionary<string, Tuple<double, double>>>();
            var accountsCntr = 0;
            foreach (var x in _portfolioBillings)
            {
                result.Add(x.Key, x.Value);
                x.Value.ForEach(_ => accountsCntr++);
            }

            Sender.Tell(new PortfolioBillingStatus(result));
            _log.Info(
                $"[GetBillingProgress]: ReplyWithBillingProgress to {Sender.Path.Name} with {result.Count} portfolios with a total of {accountsCntr} billed accounts.");
        }

        protected override void PostStop()
        {
            Context.IncrementActorStopped();
        }

        protected override void PreStart()
        {
            Context.IncrementActorCreated();
            RegisterStartup();
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

            var clientName = client.ClientName.ToUpper();
            var boardingActor = Context.ActorOf<BoardAccountActor>($"Client{clientName}");
            boardingActor.Tell(client);
            _log.Info($"[RunSimulator]: Started the boarding of accounts for Client{clientName} {DateTime.Now} ");
        }

        private Dictionary<string, string> DictionaryToStringList()
        {
            var viewble = new Dictionary<string, string>();
            foreach (var a in _portfolios)
                viewble.Add(a.Key.ToLowerInvariant(), a.Value?.ToString() ?? "Not Instantiated");
            return viewble;
        }

        private void StartPortfolios()
        {
            Monitor();
            var immutPortfolios = _portfolios.Keys.ToList();
            foreach (var portfolio in immutPortfolios)
            {
                if (_portfolios[portfolio] == null)
                {
                    var actor = InstantiateThisPortfolio(portfolio);
                    actor.Tell(new StartAccounts());
                }
                else
                {
                    _portfolios[portfolio].Tell(new StartAccounts());
                }
            }

            GetMyStatus();
        }

        private void GetMyStatus()
        {
            var tooMany = new Dictionary<string, string> {{"sorry", "Too many portfolios to list here"}};

            Sender.Tell(new MySystemStatus($"{_portfolios.Count} portfolio(s) started.",
                _portfolios.Count > 999 ? tooMany : DictionaryToStringList()));
            
        }

        private void ProcessSupervision(SuperviseThisPortfolio command)
        {
            var portfolioName = command.PortfolioName;
            Monitor();
            if (!_portfolios.ContainsKey(portfolioName))
            {
                var @event = new PortfolioAddedToSupervision(portfolioName);
                Persist(@event, s =>
                {
                    _portfolios.Add(portfolioName, null);
                    Sender.Tell(InstantiateThisPortfolio(portfolioName));
                    ApplySnapShotStrategy();
                });
            }
            else
            {
                _log.Info($"[ProcessSupervision]: You tried to load portfolio {portfolioName} which has already been loaded");
            }
        }

        private void ReplayEvent(string portfolioNumber)
        {
            RecoveryCounter();
            if (string.IsNullOrEmpty(portfolioNumber)) throw new Exception("Why is this blank?");

            if (_portfolios.ContainsKey(portfolioNumber))
            {
                _log.Debug($"[ReplayEvent]: Supervisor already has {portfolioNumber} in state. No action taken");
            }
            else
            {
                _portfolios.Add(portfolioNumber, null);
                _log.Debug($"[ReplayEvent]: Replayed event on {portfolioNumber}");
            }
        }

        private IActorRef InstantiateThisPortfolio(string portfolioName)
        {
            if (!_portfolios.ContainsKey(portfolioName))
            {
                throw new Exception("[InstantiateThisPortfolio]: Why are you trying to " +
                                    "instantiate a portfolio not yet registered?");
            }
            var portfolioActor = Context.ActorOf(Props.Create<PortfolioActor>(), portfolioName);
            _portfolios[portfolioName] = portfolioActor;
            portfolioActor.Tell(new CheckYoSelf()); // to instantiate actor
            _log.Debug($"[InstantiateThisPortfolio]: Instantiated portfolio {portfolioActor.Path.Name}");
            return portfolioActor;

        }

        private void ProcessSnapshot(SnapshotOffer offer)
        {
            Monitor();

            //var snap = ((Newtonsoft.Json.Linq.JArray) offer.Snapshot).ToObject<string[]>();
            var snap = (string[]) offer.Snapshot;

            foreach (var portfolio in snap)
            {
                _portfolios.Add(portfolio, null);
                _log.Info($"[ProcessSnapshot]: {Self.Path.Name} Snapshot recovered portfolio: {portfolio}.");
            }
        }

        public void ApplySnapShotStrategy()
        {
            if (LastSequenceNr % LoanerActors.TakeSystemSupervisorSnapshotAt != 0)
            {
                return;
            }
            
            var state = new List<string>(); // Just need the name to kick it off?

            foreach (var record in _portfolios.Keys)
            {
                state.Add(record);
            }
            
            _log.Info($"[ApplySnapShotStrategy]: SystemSupervisor Snapshot " +
                      $"taken of {state.Count} portfolios. LastSequenceNr is {LastSequenceNr}.");
            Context.IncrementCounter("SnapShotTaken");

            SaveSnapshot(state.ToArray());
        }
    }
}
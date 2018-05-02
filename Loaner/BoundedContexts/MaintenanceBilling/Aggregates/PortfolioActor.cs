using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Akka.Actor;
using Akka.Dispatch;
using Akka.Event;
using Akka.Monitoring;
using Akka.Persistence;
using Akka.Util.Internal;
using Loaner.ActorManagement;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;
using Loaner.KafkaProducer.Commands;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    using static LoanerActors;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class PortfolioActor : ReceivePersistentActor
    {
        private static int _messagesReceived;
        
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private readonly PropertyInfo _numberOfMessagesProperty =
            typeof(Mailbox).GetProperty("NumberOfMessages", BindingFlags.Instance | BindingFlags.NonPublic);

        private PorfolioState _porfolioState = new PorfolioState();

        private Stopwatch _stopWatch;

        private Dictionary<string,BusinessRuleApplicationResultModel> _failedAccounts = new Dictionary<string, BusinessRuleApplicationResultModel>();

        public PortfolioActor()
        {
            /*** recovery section **/
            Recover<SnapshotOffer>(offer => ProcessSnapshot(offer));
            Recover<AccountUnderSupervisionBalanceChanged>(cmd => UpdateAccountUnderSupervisionBalance(cmd));

            /** Core Commands **/
            Command<SuperviseThisAccount>(command => ProcessSupervision(command));
            Command<StartAccounts>(command => StartAccounts());
            Command<AssessWholePortfolio>(cmd => AssessAllAccounts(cmd));
            Command<CheckYoSelf>(cmd => RegisterStartup());
            Command<FailedAccountBillingAssessment>(cmd => _HandleFailedBillingAssessment(cmd));
            Command<GetFailedBilledAccounts>(cmd => _GetFailedBillingAssessment());

            
            /* Common comands */
            Command<TellMeYourStatus>(asking => GetMyStatus());
            Command<TellMeAboutYou>(me => AboutMe(me));
            Command<TellMeYourPortfolioStatus>(msg => _log.Debug(msg.Message));
            Command<RegisterMyAccountBalanceChange>(cmd => RegisterBalanceChange(cmd));

            Command<PublishPortfolioStateToKafka>(cmd => PublishToKafka(cmd));
            Command<ReportDebugInfo>(cmd => ReportDebugInfo(cmd));
            Command<ReportPortfolioStateToParent>(cmd => ReportPortfolioStateToParent());
            Command<ReportMailboxSize>(cmd => ReportMailboxSize());

            /** Special handlers below; we can decide how to handle snapshot processin outcomes. */
            Command<SaveSnapshotSuccess>(success => PurgeOldSnapShots(success));
            Command<DeleteSnapshotsSuccess>(msg =>  Monitor() );
            Command<SaveSnapshotFailure>(msg => SnapShotSaveError(msg.Cause.Message));
            Command<DeleteMessagesSuccess>( msg => MessageDeleteSuccess(msg));
            Command<MyAccountStatus>(msg => ReportMyAccountStatus(msg));
            Command<string>(noMessage => { });
            CommandAny(msg => _log.Error($"Unhandled message in {Self.Path.Name}. Message:{msg.ToString()}"));
        }

        private void _GetFailedBillingAssessment()
        {
            Sender.Tell( new FailedListOfAccounts(_failedAccounts.Values.ToList()));
        }

        private void _HandleFailedBillingAssessment(FailedAccountBillingAssessment cmd)
        {
            _failedAccounts.AddOrSet(cmd.AccountNumber, cmd.ApplicationResult);
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            // or AllForOneStrategy
            return new OneForOneStrategy(
                maxNrOfRetries: 10,
                withinTimeRange: TimeSpan.FromSeconds(30),
//                    if (x is ArithmeticException) return Directive.Resume;
//                    else if (x is InsanelyBadException) return Directive.Escalate;
//                    else if (x is NotSupportedException) return Directive.Stop;
//                    else return Directive.Restart;
                localOnlyDecider: x =>
                {
                    _log.Error($"Restarting failed actor: {x.GetType()} {x}");
                    return Directive.Restart;
                });
        }

        private void ReportMyAccountStatus(MyAccountStatus msg)
        {
            Monitor();
            _log.Debug(
                $"Why is account {msg.AccountState.AccountNumber} sending me an 'MyAccountStatus' message?");
        }

        private void MessageDeleteSuccess(DeleteMessagesSuccess msg)
        {
            Monitor();
            _log.Debug($"Successfully cleared log after snapshot ({msg.ToString()})");
        }

        private void SnapShotSaveError(string reason)
        {
            Monitor();
            
            _log.Error(
                $"Actor {Self.Path.Name} was unable to save a snapshot. {reason}");
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
                TimeSpan.FromSeconds(30), Self, new ReportDebugInfo(), ActorRefs.NoSender);

//            DemoActorSystem.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0),
//                TimeSpan.FromSeconds(10), Self, new ReportPortfolioStateToParent(), ActorRefs.NoSender);

//            DemoActorSystem.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0),
//                TimeSpan.FromSeconds(30), Self, new PublishPortfolioStateToKafka(), ActorRefs.NoSender);

//            DemoActorSystem.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0),
//                TimeSpan.FromSeconds(30), Self, new ReportMailboxSize(), ActorRefs.NoSender);
        }

        private void AboutMe(TellMeAboutYou me)
        {
            Monitor();
            Console.WriteLine(
                $"About me: I am {Self.Path.Name} Msg: {me.Me} I was last booted up on: {_porfolioState.LastBootedOn}");
        }

        private void PurgeOldSnapShots(SaveSnapshotSuccess success)
        {
            _log.Info($"[PurgeOldSnapShots]: Portfolio {Self.Path.Name} got SaveSnapshotSuccess " +
                      $"at SequenceNr {success.Metadata.SequenceNr} Current SequenceNr is {LastSequenceNr}.");
            Monitor();
            
            //var snapshotSeqNr = success.Metadata.SequenceNr;
            // delete all messages from journal and snapshot store before latests confirmed
            // snapshot, we won't need them anymore
            //DeleteMessages(snapshotSeqNr);
            //DeleteSnapshots(new SnapshotSelectionCriteria(snapshotSeqNr - 1));
        }

        private void RegisterBalanceChange(RegisterMyAccountBalanceChange cmd)
        {
            Monitor();

            _stopWatch = Stopwatch.StartNew();
            _stopWatch.Start();

            var account =
                _porfolioState.SupervizedAccounts.FirstOrDefault(x => x.Key == cmd.AccountNumber).Value ??
                new AccountUnderSupervision(cmd.AccountNumber, cmd.AccountBalanceAfterTransaction);

            _log.Debug(
                $"[RegisterBalanceChange]: account.BalanceAfterLastTransaction={account.BalanceAfterLastTransaction}\t" +
                $"cmd.AccountBalanceAfterTransaction={cmd.AccountBalanceAfterTransaction}\n" +
                $"account.LastBilledAmount={account.LastBilledAmount}\tcmd.AmountTransacted={cmd.AmountTransacted}");

            account.LastBilledAmount = cmd.AmountTransacted;
            account.BalanceAfterLastTransaction = cmd.AccountBalanceAfterTransaction;

            _porfolioState.SupervizedAccounts.AddOrSet(cmd.AccountNumber, account);
            var before = _stopWatch.ElapsedMilliseconds;
            _porfolioState.UpdateBalance();
            var after = _stopWatch.ElapsedMilliseconds;

            

            var @event =
                new AccountUnderSupervisionBalanceChanged(
                    account.AccountNumber
                    , account.BalanceAfterLastTransaction
                );

            _stopWatch.Stop();
            if (after - before >= 2)
            {
                ReportStopwatchInfo($"RegisterBalanceChange()/UpdateBalance() timediff {after-before}ms",_stopWatch.ElapsedMilliseconds);
            }
            else
            {
                ReportStopwatchInfo($"RegisterBalanceChange()", _stopWatch.ElapsedMilliseconds);
            }

            Persist(@event, s => ApplySnapShotStrategy());


            //Self.Tell(new PublishPortfolioStateToKafka());
        }

        private void ReportStopwatchInfo(string methodName, long miliseconds)
        {

            _log.Debug($"PortfolioActor: {methodName} - {miliseconds}ms. Message #{_messagesReceived}");
            
        }

        private void UpdateAccountUnderSupervisionBalance(AccountUnderSupervisionBalanceChanged cmd)
        {
            Monitor();

            _stopWatch = Stopwatch.StartNew();
            _stopWatch.Start();

            var account =
                _porfolioState.SupervizedAccounts.FirstOrDefault(x => x.Key == cmd.AccountNumber).Value ??
                new AccountUnderSupervision(cmd.AccountNumber, cmd.NewAccountBalance);

            account.BalanceAfterLastTransaction = cmd.NewAccountBalance;
            _porfolioState.SupervizedAccounts.AddOrSet(cmd.AccountNumber, account);
            _porfolioState.UpdateBalance();

            _stopWatch.Stop();
            ReportStopwatchInfo("UpdateAccountUnderSupervisionBalance",_stopWatch.ElapsedMilliseconds);

        }

        private void RegisterStartup()
        {
            Monitor();
            _porfolioState.LastBootedOn = DateTime.Now;
        }

        private void AssessAllAccounts(AssessWholePortfolio cmd)
        {
            Monitor();

            _stopWatch = Stopwatch.StartNew();
            _stopWatch.Start();

            Console.WriteLine(
                $"Billing Items: {cmd.Items.Aggregate("", (acc, next) => acc + " " + next.Item.Name + " " + next.Item.Amount)}");
            try
            {
                foreach (var account in _porfolioState.SupervizedAccounts.Values.ToList())
                {
                    var bill = new BillingAssessment(
                        account.AccountNumber
                        , cmd.Items
                        , AccountBusinessRulesHandlerRouter
                        , AccountBusinessRulesMapperRouter
                    );
                    account.AccountActorRef.Tell(bill);
                    //_log.Info($"[AssessAllAccounts]: Just told account {account.AccountNumber} to run assessment.");
                }

                Sender.Tell(new TellMeYourPortfolioStatus(
                    $"Your request was sent to all {_porfolioState.SupervizedAccounts.Count.ToString()} accounts",
                    null));
            }
            catch (Exception e)
            {
                _log.Error($"[AssessAllAccounts]: {e.Message} {e.StackTrace}");
                throw;
            }

            _stopWatch.Stop();
            ReportStopwatchInfo("AssessAllAccounts",_stopWatch.ElapsedMilliseconds);
            
        }


        private void Monitor()
        {
            if (_messagesReceived++ % 1000 == 0)
            {
                _log.Info($"PortfolioActor: Monitor() - Recieved {_messagesReceived} messages.");
            }

            Context.IncrementMessagesReceived();
        }


        private void StartAccounts()
        {
            Monitor();

            _stopWatch = Stopwatch.StartNew();
            _stopWatch.Start();

            var immutAccounts = _porfolioState.SupervizedAccounts.Values.ToImmutableList();

            foreach (var account in immutAccounts)
            {
                // Since accounts algo get instantiated when portfolio starts up/recovers
                if (account.AccountActorRef != null)
                {
                    continue;
                }
                account.AccountActorRef = InstantiateThisAccount(account);
                account.AccountActorRef.Tell(new PublishAccountStateToKafka());
            }

            Sender.Tell(
                new TellMeYourPortfolioStatus(
                    $"{_porfolioState.SupervizedAccounts.Count} accounts. " +
                    $"I was last booted up on: {_porfolioState.LastBootedOn}",
                    null));

            _stopWatch.Stop();
          
            ReportStopwatchInfo("StartAccounts",_stopWatch.ElapsedMilliseconds);
            Self.Tell(new PublishPortfolioStateToKafka());

        }

        private void GetMyStatus()
        {
            Monitor();

            _stopWatch = Stopwatch.StartNew();
            _stopWatch.Start();

            var portfolioSate = new PortfolioStateViewModel
            {
                AccountCount = _porfolioState.SupervizedAccounts.Count,
                AsOfDate = DateTime.Now,
                PortfolioName = Self.Path.Name,
                TotalBalance = _porfolioState.CurrentPortfolioBalance
            };

            Sender.Tell(new TellMeYourPortfolioStatus(
                $"{_porfolioState.SupervizedAccounts.Count} accounts. " +
                $"I was last booted up on: {_porfolioState.LastBootedOn:yyyy-MM-dd h:mm tt}",
                portfolioSate));

            _stopWatch.Stop();
            ReportStopwatchInfo("GetMyStatus",_stopWatch.ElapsedMilliseconds);

        }

        private void ProcessSupervision(SuperviseThisAccount command)
        {
            Monitor();

            var account = new AccountUnderSupervision(command.AccountNumber, command.CurrentAccountBalance);
            var @event = new AccountAddedToSupervision(command.AccountNumber, (decimal) command.CurrentAccountBalance);
            Persist(@event, s =>
            {
                _stopWatch = Stopwatch.StartNew();
                _stopWatch.Start();

                account.AccountActorRef = InstantiateThisAccount(account);
                _porfolioState.SupervizedAccounts.AddOrSet(command.AccountNumber, account);

                //Self.Tell(new PublishPortfolioStateToKafka());

                _stopWatch.Stop();
                
                ReportStopwatchInfo($"ProcessSupervision()/Persist()",_stopWatch.ElapsedMilliseconds);

                ApplySnapShotStrategy();
            });
        }


        private IActorRef InstantiateThisAccount(AccountUnderSupervision account)
        {
            var accountActor = Context.ActorOf(Props.Create<AccountActor>(), account.AccountNumber);

            account.AccountActorRef = accountActor; // is this needed?

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
            if (LastSequenceNr % TakePortolioSnapshotAt != 0) return;
            var clonedState = _porfolioState.Clone();
            _log.Info(
                $"[ApplySnapShotStrategy]: Portfolio {Self.Path.Name} snapshot taken. Current SequenceNr is {LastSequenceNr}.");
            Context.IncrementCounter("SnapShotTaken");
            SaveSnapshot(clonedState);
        }

        #region MethodsCalledOnATimer

        private void ReportMailboxSize()
        {
            _log.Info($"[ReportMailboxSize]: {PersistenceId} Mailbox Size: {GetMailboxSize():##,#}");
        }

        private int GetMailboxSize()
        {
            Monitor();

            if (!(Context is ActorCell context))
                return 0;

            var mailbox = context.Mailbox;
            var numberOfMessages = (int) _numberOfMessagesProperty.GetValue(mailbox);

            return numberOfMessages;
        }

        private void ReportPortfolioStateToParent()
        {
            Monitor();

            var viewble = new Dictionary<string, Tuple<double, double>>();
            foreach (var a in _porfolioState.SupervizedAccounts?.Values.ToList())
                viewble.Add(a.AccountNumber, Tuple.Create(a.LastBilledAmount, a.BalanceAfterLastTransaction));
            Context.Parent.Tell(new RegisterPortolioBilling(Self.Path.Name, viewble));
            var totalBal = viewble.Aggregate(0.0, (x, y) => x + y.Value.Item2);

            _log.Debug($"[ReportPortfolioStateToParent]: {Self.Path.Name} sent {Context.Parent.Path.Name} portfolio" +
                       $" billing message containing {viewble.Count:##,#} billed accounts and a balance of {totalBal:C} ");
        }

        private void ReportDebugInfo(ReportDebugInfo msg)
        {
            Monitor();

            var active = _porfolioState.SupervizedAccounts.Count(x => x.Value.AccountActorRef != null);

            var totalBillings =
                _porfolioState.SupervizedAccounts.Aggregate(0.0, (x, y) => x + y.Value.BalanceAfterLastTransaction);

            int accounts = _porfolioState.SupervizedAccounts.Count;
            _log.Info(
                $"[ReportDebugInfo]: {Self.Path.Name} has {accounts:##,#} " +
                $"accounts under supervision, " +
                $"of which, {active:##,#} are active " +
                $"with a total balance of {totalBillings:C}" +
                $" (report#{_porfolioState.ScheduledCallsToInfo++})");
        }

        private void PublishToKafka(PublishPortfolioStateToKafka cmd)
        {
            Monitor();

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

        #endregion
    }

    public class GetFailedBilledAccounts
    {
    }

    public class FailedAccountBillingAssessment
    {
        public FailedAccountBillingAssessment(string accountNumber, BusinessRuleApplicationResultModel applicationResult)
        {
            AccountNumber = accountNumber;
            ApplicationResult = applicationResult;
        }
        
        public string AccountNumber { get; private set; }
        public BusinessRuleApplicationResultModel ApplicationResult  { get; private set; }
        
    }
}
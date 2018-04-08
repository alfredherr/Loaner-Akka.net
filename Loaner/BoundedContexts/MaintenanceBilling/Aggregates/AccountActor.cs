using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using Akka.Persistence;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;
using Loaner.KafkaProducer.Commands;
using static Loaner.ActorManagement.LoanerActors;


namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AccountActor : ReceivePersistentActor
    {
          
        private static int _messagesReceived;
        
        private readonly ILoggingAdapter _log = Context.GetLogger();

        /* This Actor's State */
        private AccountState _accountState = new AccountState();


        private string ParentName = ""; //hack to  pass porfolio name to kafka upon boarding -
        //TODO move to portfolio state maybe? Should the account know about its parent in its state? Hmm.

        private DateTime _lastBootedOn;

        public AccountActor()
        {
            /* Hanlde Recovery */
            Recover<SnapshotOffer>(offer => offer.Snapshot is AccountState, offer => ApplySnapShot(offer));
            Recover<AccountCreated>(@event => ApplyPastEvent("AccountCreated", @event));
            Recover<ObligationAddedToAccount>(@event => ApplyPastEvent("ObligationAddedToAccount", @event));
            Recover<ObligationAssessedConcept>(@event => ApplyPastEvent("ObligationAssessedConcept", @event));
            Recover<SuperSimpleSuperCoolDomainEventFoundByRules>(
                @event => ApplyPastEvent("SuperSimpleSuperCoolEventFoundByRules", @event));
            Recover<PaymentAppliedToObligation>(
                @event => ApplyPastEvent("PaymentAppliedToObligation", @event));

            /**
             * Creating the account's initial state is more of a one-time thing 
             * For the demo there no business rules are assumed when adding an 
             * maintenanceFee to an account, but there most likely will be in reality
             * */
            Command<CreateAccount>(command => InitiateAccount(command));
            Command<AddObligationToAccount>(command => AddObligation(command));
            Command<CheckYoSelf>(command => RegisterStartup() /*effectively a noop */);

            /* Example of running comannds through business rules */
            Command<BillingAssessment>(command => ProcessBilling(command));
            Command<BusinessRuleApplicationResultModel>(model => ApplyBusinessRules(model));
            Command<PayAccount>(cmd => ProcessPayment(cmd));
            Command<AskToBeSupervised>(command => GetSupervised(command));
            Command<PublishAccountStateToKafka>(msg => PublishToKafka(msg));
            Command<CompleteBoardingProcess>(msg => CompleteBoardingProcess());

            /** Special handlers below; we can decide how to handle snapshot processin outcomes. */
            Command<SaveSnapshotSuccess>(success => PurgeOldSnapShots(success));
            Command<DeleteSnapshotsSuccess>(msg => { });
            Command<SaveSnapshotFailure>(
                failure => _log.Error(
                    $"[SaveSnapshotFailure]: Actor {Self.Path.Name} was unable to save a snapshot. {failure.Cause.Message}"));
            
            Command<TellMeYourStatus>(asking =>
                Sender.Tell(
                    new MyAccountStatus(
                        $"{Self.Path.Name} I am alive! I was last booted up on {_lastBootedOn:yyyy-MM-dd hh:mm:ss}")));
            
            Command<TellMeYourInfo>(
                asking => Sender.Tell(new MyAccountStatus("", (AccountState) _accountState.Clone())));

            Command<DeleteMessagesSuccess>(
                msg => _log.Debug(
                    $"[DeleteMessagesSuccess]: Successfully cleared log after snapshot ({msg.ToString()})"));
            
            CommandAny(msg =>
                _log.Error($"[CommandAny]: Unhandled message in {Self.Path.Name} from {Sender.Path.Name}. Message:{msg.ToString()}"));
        }

        protected override void PreRestart(Exception reason, object message)
        {
            // put message back in mailbox for re-processing after restart
            _log.Error($"Restarting {Self.Path.Name} and reprocessing message in trasit {message.GetType()}");
            Self.Tell(message);
        }
        
        private void ProcessPayment(PayAccount cmd)
        {
            try
            {
                var luckObligation = _accountState.Obligations.FirstOrDefault(x => x.Key == "AccountAdjustments").Value;
                if (luckObligation == null)
                    throw new Exception($"[ProcessPayment]: Inconvibable! Why is there no obligation?");

                var @event = new PaymentAppliedToObligation(
                    luckObligation.ObligationNumber
                    , new CreditCardPayment(cmd.AmountToPay)
                    , cmd.AmountToPay
                    , "CreditCard Payment Applied To Dues"
                );
                Persist(@event, s =>
                {
                    _accountState = _accountState.ApplyEvent(@event);
                    Self.Tell(new PublishAccountStateToKafka());
                    ApplySnapShotStrategy();
                    Sender.Tell(new MyAccountStatus("Payment Applied",(AccountState) _accountState.Clone()) );
                    
                });
                
            }
            catch (Exception e)
            {
                _log.Error($"[ProcessPayment]: {e.Message} {e.StackTrace}");
                throw;
            }
        }


        public override string PersistenceId => Self.Path.Name;

        private void PublishToKafka(PublishAccountStateToKafka msg)
        {

            
            ParentName = Self.Path.Parent.Name.Contains("$") ? ParentName : Self.Path.Parent.Name;
 
            var kafkaAccountModel = new AccountStateViewModel
            (
                accountNumber: _accountState.AccountNumber,
                userName: _accountState.UserName,
                portfolioName: ParentName, //Self.Path.Parent.Name,
                currentBalance: _accountState.CurrentBalance,
                accountStatus: _accountState.AccountStatus,
                asOfDate: DateTime.Now,
                lastPaymentDate: _accountState.LastPaymentDate,
                lastPaymentAmount: (decimal) _accountState.LastPaymentAmount,
                daysDelinquent: DateTime.Now.Subtract(_accountState.LastPaymentDate).Days,
                accountInventory: _accountState.Inventroy
            );

            AccountStatePublisherActor.Tell(new Publish(kafkaAccountModel.AccountNumber, kafkaAccountModel));
            _log.Debug($"[PublishToKafka]: Sending kafka message for account {kafkaAccountModel}");
        }

        private void PurgeOldSnapShots(SaveSnapshotSuccess success)
        {
            //_log.Info($"[PurgeOldSnapShots]: Account {Self.Path.Name} got SaveSnapshotSuccess " +
            //          $"at SequenceNr {success.Metadata.SequenceNr} Current SequenceNr is {LastSequenceNr}.");

            //var snapshotSeqNr = success.Metadata.SequenceNr;
            // delete all messages from journal and snapshot store before latests confirmed
            // snapshot, we won't need them anymore
            //DeleteMessages(snapshotSeqNr);
            //DeleteSnapshots(new SnapshotSelectionCriteria(snapshotSeqNr - 1));
        }

        private void RegisterStartup()
        {
            _lastBootedOn = DateTime.Now;
        }

//        private void ReportMyState(double transAmount, double balanceAfter, IActorRef toWhom = null)
//        {
//            if (toWhom == null)
//                toWhom = Context.Parent; // use the parent if we're not passed one.
//
//            toWhom.Tell(
//                new RegisterMyAccountBalanceChange(
//                    _accountState.AccountNumber,
//                    transAmount,
//                    balanceAfter)
//            );
//        }

        private void ProcessBilling(BillingAssessment command)
        {
            if (_accountState?.AccountNumber == null)
            {
                _log.Error($"[ProcessBilling]: Actor {Self.Path.Name} is passing an empty account number.");
                throw new Exception($"[ProcessBilling]: Actor {Self.Path.Name} is passing an empty account number.");
            }

            try
            {
                var c = command;
                var billedAmount = c.LineItems.Aggregate(0.0, (accumulator, next) => accumulator + next.Item.Amount);

//                var parameters = c.LineItems.Aggregate("",
//                    (working, next) => working + ";" + next.Item.Name + "=" + next.Item.Amount);

                var model =
                    new ApplyBusinessRules(
                        client: Self.Path.Parent.Parent.Name,
                        portfolioName: Self.Path.Parent.Name,
                        accountState: (AccountState) _accountState.Clone(),
                        command: command,
                        totalBilledAmount: billedAmount,
                        accountRef: Self
                    );

                command.AccountBusinessMapperRouter.Tell(model);
                //_log.Info($"[ProcessBilling]: {response}");
            }
            catch (Exception e)
            {
                _log.Error($"[ProcessBilling]: {Self.Path.Name} {e.Message} {e.StackTrace}");
                throw;
            }
        }

        private void GetSupervised(AskToBeSupervised command)
        {
            Monitor();
            /* Assuming this is all we have to load for an account, then we can have the account
             * send the supervisor to add it to it's list -- then it can terminate. 
             */
            command.MyNewParent.Tell(new SuperviseThisAccount(
                command.Portfolio
                , Self.Path.Name
                , (double) _accountState.CurrentBalance)
            );

            this.ParentName = command.Portfolio;
            //ReportMyState(0,(double) _accountState.CurrentBalance, command.MyNewParent);

            //Report current state to Kafka
            Self.Tell(new PublishAccountStateToKafka());
            
            //before stopping
            Self.Tell(new CompleteBoardingProcess());
        }

        private void CompleteBoardingProcess()
        {
            //Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(2),Self,PoisonPill.Instance, ActorRefs.NoSender );
            Self.Tell(PoisonPill.Instance, ActorRefs.NoSender);
        }

        private void ApplySnapShot(SnapshotOffer offer)
        {
            _accountState = (AccountState) offer.Snapshot;
            //_log.Info($"{Self.Path.Name} Snapshot recovered.");
        }

        private void ApplyPastEvent(string eventname, IDomainEvent domainEvent)
        {
            RecoveryCounter();
            _accountState = _accountState.ApplyEvent(domainEvent);
            _log.Debug($"[ApplyPastEvent]: Recovery event: {eventname}");
        }

        private void AddObligation(AddObligationToAccount command)
        {
            Monitor();
            if (!_accountState.Obligations.ContainsKey(command.MaintenanceFee.ObligationNumber))
            {
                var @event = new ObligationAddedToAccount(command.AccountNumber, command.MaintenanceFee);
                Persist(@event, s =>
                {
                    _accountState = _accountState.ApplyEvent(@event);
                    ApplySnapShotStrategy();
                    _log.Debug(
                        $"[AddObligation]: Added maintenanceFee {command.MaintenanceFee.ObligationNumber} to account {command.AccountNumber}");
                    
                    //Report current state to Kafka
                    Self.Tell(new PublishAccountStateToKafka());
                    
                });
            }
            else
            {
                _log.Debug(
                    $"[AddObligation]: You are trying to add maintenanceFee {command.MaintenanceFee.ObligationNumber} an account which has exists on account {command.AccountNumber}. No action taken.");
            }
        }

        private void InitiateAccount(CreateAccount command)
        {
            Monitor();
            if (_accountState.AccountNumber != null)
            {
                _log.Warning(
                    $"[InitiateAccount]: You are trying to create {command.AccountNumber}, but has already been created. No action taken.");
                return;
            }

            /**
             * we want to use behaviours here to make sure we don't allow the account to be created 
             * once it has been created -- Become AccountBoarded perhaps?
              */

            var events = new List<IDomainEvent>
            {
                new AccountCreated
                (
                    accountNumber: command.AccountNumber,
                    openingBalance: command.BoardingModel.OpeningBalance,
                    inventory: command.BoardingModel.Inventory,
                    userName: command.BoardingModel.UserName,
                    lastPaymentDate: command.BoardingModel.LastPaymentDate,
                    lastPaymentAmount: command.BoardingModel.LastPaymentAmount
                )
            };


            if (command.BoardingModel.OpeningBalance != 0.0)
            {
                events.Add(new AccountCurrentBalanceUpdated(command.AccountNumber,
                    command.BoardingModel.OpeningBalance));
            }

            foreach (var @event in events)
            {
                Persist(@event, s =>
                {
                    _accountState = _accountState.ApplyEvent(@event);
                    ApplySnapShotStrategy();
                    
                });
            }
        }

        private void ApplyBusinessRules(BusinessRuleApplicationResultModel resultModel)
        {
            Monitor();
            /**
			 * Here we can call Business Rules to validate and do whatever.
			 * Then, based on the outcome generated events.
			 * In this example, we are simply going to accept it and updated our state.
			 */

            _log.Debug(
                $"[ApplyBusinessRules]: There were {resultModel.GeneratedEvents.Count} events. And success={resultModel.Success}");

            if (!resultModel.Success)
            {
                _log.Error($"{Self.Path.Name} Business Rule Validation Failure.");
                return;
            }
            
            /* I may want to do old vs new state comparisons for other reasons
                 *  but ultimately we just update the state.. */
            var events = resultModel.GeneratedEvents;

            foreach (var @event in events)
                Persist(@event, s =>
                {
                    _accountState = _accountState.ApplyEvent(@event);

                    _log.Debug(
                        $"[ApplyBusinessRules]: Persisted event {@event.GetType().Name} on account {_accountState.AccountNumber}" +
                        $" account balance after is {_accountState.CurrentBalance:C} ");

                    //ReportMyState(resultModel.TotalBilledAmount, (double) _accountState.CurrentBalance);

                    //Report current state to Kafka
                    Self.Tell(new PublishAccountStateToKafka());

                    if (_messagesReceived++ % 10000 == 0)
                    {
                        _log.Info(
                            $"AccountActor: ApplyBusinessRules()/Persist() {PersistenceId} No. Events {events.Count}.");
                    }

                    ApplySnapShotStrategy();
                });
        }

        /*Example of how snapshotting can be custom to the actor, in this case per 'Account' events*/
        public void ApplySnapShotStrategy()
        {
            if (LastSequenceNr % TakeAccountSnapshotAt != 0)
            {
                return;
            }
            
            var clonedState = _accountState.Clone();
            
            SaveSnapshot(clonedState);
            
            Context.IncrementCounter("SnapShotTaken");
        }

        private void Monitor()
        {
            Context.IncrementMessagesReceived();
        }

        protected override void PostStop()
        {
            Context.IncrementActorStopped();
        }

        protected override void PreStart()
        {
            Context.IncrementActorCreated();
        }

        private void RecoveryCounter()
        {
            Context.IncrementCounter("AccountRecovery");
        }
    }
}
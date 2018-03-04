using System;
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
using Loaner.KafkaProducer.Commands;
using static Loaner.ActorManagement.LoanerActors;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    public class AccountActor : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        /* This Actor's State */
        private AccountState _accountState = new AccountState();


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

            /**
             * Creating the account's initial state is more of a one-time thing 
             * For the demo there no business rules are assumed when adding an 
             * maintenanceFee to an account, but there most likely will be in reality
             * */
            Command<CreateAccount>(command => InitiateAccount(command));
            Command<AddObligationToAccount>(command => AddObligation(command));
            Command<CheckYoSelf>(command => RegisterStartup() /*effectively a noop */);

            /* Example of running comannds through business rules */
//            Command<SettleFinancialConcept>(command => ApplyBusinessRules(command));
//            Command<AssessFinancialConcept>(command => ApplyBusinessRules(command));
            Command<BillingAssessment>(command => ProcessBilling(command));
            Command<BusinessRuleApplicationResultModel>(model => ApplyBusinessRules(model) );
//            Command<CancelAccount>(command => ApplyBusinessRules(command));
            Command<AskToBeSupervised>(command => SendParentMyState(command));
            Command<PublishAccountStateToKafka>(msg => PublishToKafka(msg));
            Command<CompleteBoardingProcess>(msg => CompleteBoardingProcess());

            /** Special handlers below; we can decide how to handle snapshot processin outcomes. */
            Command<SaveSnapshotSuccess>(success => PurgeOldSnapShots(success));
            Command<DeleteSnapshotsSuccess>(msg => { });
            Command<SaveSnapshotFailure>(
                failure => _log.Error(
                    $"[SaveSnapshotFailure]: Actor {Self.Path.Name} was unable to save a snapshot. {failure.Cause.Message}"));
            //Command<RecoverySuccess>(msg => this.WakeUp());
            Command<TellMeYourStatus>(asking =>
                Sender.Tell(
                    new MyAccountStatus(
                        $"{Self.Path.Name} I am alive! I was last booted up on {_lastBootedOn.ToString("yyyy-MM-dd hh:mm:ss")}")));
            Command<TellMeYourInfo>(asking => Sender.Tell(new MyAccountStatus("", (AccountState) _accountState.Clone())));

            Command<DeleteMessagesSuccess>(
                msg => _log.Debug($"[DeleteMessagesSuccess]: Successfully cleared log after snapshot ({msg.ToString()})"));
            CommandAny(msg => _log.Error($"[CommandAny]: Unhandled message in {Self.Path.Name}. Message:{msg.ToString()}"));
        }


        public override string PersistenceId => Self.Path.Name;

        private void PublishToKafka(PublishAccountStateToKafka msg)
        {
            var kafkaAccountModel = new AccountStateViewModel
            (
                _accountState.AccountNumber,
                _accountState.UserName,
                Self.Path.Parent.Name,
                (decimal) _accountState.CurrentBalance,
                _accountState.AccountStatus,
                DateTime.Now,
                _accountState.LastPaymentDate,
                (decimal) _accountState.LastPaymentAmount,
                DateTime.Now.Subtract(_accountState.LastPaymentDate).Days,
                _accountState.Inventroy
            );

            AccountStatePublisherActor.Tell(new Publish(kafkaAccountModel.AccountNumber, kafkaAccountModel));
            _log.Debug($"[PublishToKafka]: Sending kafka message for account {kafkaAccountModel}");
        }

        private void PurgeOldSnapShots(SaveSnapshotSuccess success)
        {
            var snapshotSeqNr = success.Metadata.SequenceNr;
            // delete all messages from journal and snapshot store before latests confirmed
            // snapshot, we won't need them anymore
            DeleteMessages(snapshotSeqNr);
            DeleteSnapshots(new SnapshotSelectionCriteria(snapshotSeqNr - 1));
        }

        private void RegisterStartup()
        {
            _lastBootedOn = DateTime.Now;
        }

        private void ReportMyState(double transAmount, double balanceAfter, IActorRef toWhom = null)
        {
            if (toWhom == null)
                toWhom = Context.Parent; // use the parent if we're not passed one.

            toWhom.Tell(
                new RegisterMyAccountBalanceChange(
                    _accountState.AccountNumber,
                    transAmount,
                    balanceAfter)
            );
        }

        private void ProcessBilling(BillingAssessment command)
        {
         
            if (_accountState.AccountNumber == null)
                throw new Exception($"Actor {Self.Path.Name} is passing an empty account number.");

            var billedAmount = 0.0;
            
            var c = command;
            billedAmount = c.LineItems.Aggregate(0.0, (accumulator, next) => accumulator + next.Item.Amount);

            var parameters = c.LineItems.Aggregate("",
                (working, next) => working + ";" + next.Item.Name + "=" + next.Item.Amount);
            _log.Debug($"[ApplyBusinessRules]: {_accountState.AccountNumber}: " +
                       $"Command {c.GetType().Name} with {c.LineItems.Count} total billed = {billedAmount} & line items: " +
                       $"{parameters}");
             
           
            var model = new ApplyBusinessRules
            {
                Client = Self.Path.Parent.Parent.Name,
                PortfolioName = Self.Path.Parent.Name,
                AccountState = (AccountState) _accountState.Clone(),
                Command = command,
                TotalBilledAmount = billedAmount
            };

            command.BusinessRulesHandlingRouter.Tell(model);
            
        }

        private void SendParentMyState(AskToBeSupervised command)
        {
            Monitor();
            /* Assuming this is all we have to load for an account, then we can have the account
             * send the supervisor to add it to it's list -- then it can terminate. 
             */
            command.MyNewParent.Tell(new SuperviseThisAccount(command.Portfolio, Self.Path.Name));

            ReportMyState(0, _accountState.CurrentBalance, command.MyNewParent);

            Self.Tell(new CompleteBoardingProcess());
        }

        private void CompleteBoardingProcess()
        {
            Self.Tell(PoisonPill.Instance);
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
                    /* Optionally, put this command on the external notificaiton system (i.e. Kafka) */
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
            if (_accountState.AccountNumber == null)
            {
                /**
                 * we want to use behaviours here to make sure we don't allow the account to be created 
                 * once it has been created -- Become AccountBoarded perhaps?
                  */
                var @event = new AccountCreated
                (
                    accountNumber: command.AccountNumber,
                    openingBalance: command.BoardingModel.OpeningBalance,
                    inventory: command.BoardingModel.Inventory,
                    userName: command.BoardingModel.UserName,
                    lastPaymentDate: command.BoardingModel.LastPaymentDate,
                    lastPaymentAmount: command.BoardingModel.LastPaymentAmount
                );
                Persist(@event, s =>
                {
                    _accountState = _accountState.ApplyEvent(@event);

                    _log.Debug($"[InitiateAccount]: Created account {command.AccountNumber}");
                });
            }
            else
            {
                _log.Warning(
                    $"[InitiateAccount]: You are trying to create {command.AccountNumber}, but has already been created. No action taken.");
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
            
//            _log.Info(
//                $"[ApplyBusinessRules]: There were {resultModel.GeneratedEvents.Count} events. And success={resultModel.Success}");

            if (resultModel.Success)
            {
                /* I may want to do old vs new state comparisons for other reasons
                 *  but ultimately we just update the state.. */
                var events = resultModel.GeneratedEvents;

                foreach (var @event in events)
                    Persist(@event, s =>
                    {
                        _accountState = _accountState.ApplyEvent(@event);

                        _log.Info(
                            $"[ApplyBusinessRules]: Persisted event {@event.GetType().Name} on account {_accountState.AccountNumber}" +
                            $" account balance after is {_accountState.CurrentBalance:C} ");

                        ReportMyState(resultModel.TotalBilledAmount, _accountState.CurrentBalance);

                        //Report current state to Kafka
                        Self.Tell(new PublishAccountStateToKafka());
                        ApplySnapShotStrategy();
                    });
            }
        }

        /*Example of how snapshotting can be custom to the actor, in this case per 'Account' events*/
        public void ApplySnapShotStrategy()
        {
            if (LastSequenceNr % TakeAccountSnapshotAt == 0)
            {
                var clonedState = _accountState.Clone();
                SaveSnapshot(clonedState);
                _log.Debug($"[ApplySnapShotStrategy]: {_accountState.AccountNumber} Snapshot taken. LastSequenceNr is {LastSequenceNr}.");
                Context.IncrementCounter("SnapShotTaken");
            }
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
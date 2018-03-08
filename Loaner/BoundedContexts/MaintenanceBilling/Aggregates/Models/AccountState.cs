using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
using Akka.Util.Internal;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Exceptions;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    public class AccountState : ICloneable
    {
        /**
         * Only two ways to initiate an Account State
         * All modifications to it must be immutable and done by the ApplyEvent() handler
         */
        public AccountState()
        {
            Obligations = ImmutableDictionary.Create<string, MaintenanceFee>();
            SimulatedFields = ImmutableDictionary.Create<string, string>();
            AuditLog = ImmutableList.Create<StateLog>();
        }

        public AccountState(string accountNumber) : this()
        {
            AccountNumber = accountNumber;
        }


        private AccountState(
            string accountNumber,
            decimal currentBalance,
            AccountStatus accountStatus,
            ImmutableDictionary<string, MaintenanceFee> obligations,
            ImmutableDictionary<string, string> simulation,
            ImmutableList<StateLog> log,
            double openingBalance,
            string inventory,
            string userName,
            double lastPaymentAmount,
            DateTime lastPaymentDate)
        {
            AccountNumber = accountNumber;
            CurrentBalance = currentBalance;
            AccountStatus = accountStatus;
            Obligations = obligations;
            AuditLog = log;
            SimulatedFields = simulation;
            Inventroy = inventory;
            UserName = userName;
            LastPaymentAmount = lastPaymentAmount;
            LastPaymentDate = lastPaymentDate;
            OpeningBalance = openingBalance;
        }

        [JsonProperty(Order = 1)]
        public string AccountNumber { get; }

        [JsonProperty(Order = 2)]
        public string UserName { get; }

        [JsonProperty(Order = 3)]
        public double LastPaymentAmount { get; }

        [JsonProperty(Order = 4)]
        public DateTime LastPaymentDate { get; }

        [JsonProperty(Order = 5)]
        public string Inventroy { get; }

        [JsonProperty(Order = 6)]
        public double OpeningBalance { get; }

        [JsonProperty(Order = 7)]
        public decimal CurrentBalance { get; }

        [JsonProperty(Order = 8)]
        [JsonConverter(typeof(StringEnumConverter))]
        public AccountStatus AccountStatus { get; private set; }

        [JsonProperty(Order = 9)]
        public ImmutableDictionary<string, MaintenanceFee> Obligations { get; }

        [JsonProperty(Order = 10)]
        public ImmutableList<StateLog> AuditLog { get; }

        [JsonProperty(Order = 11)]
        public ImmutableDictionary<string, string> SimulatedFields { get; }


        /**
         * The ApplyEvent() handler is responsible for always returning a new state
         */
        public AccountState ApplyEvent(IDomainEvent domainEvent)
        {
            switch (domainEvent)
            {
                case AccountCurrentBalanceUpdated occurred:
                    return ApplyEvent(occurred);
                case AccountStatusChanged occurred:
                    return ApplyEvent(occurred);
                case AccountCancelled occurred:
                    return ApplyEvent(occurred);
                case ObligationAddedToAccount occurred:
                    return ApplyEvent(occurred);
                case ObligationAssessedConcept occurred:
                    return ApplyEvent(occurred);
                case PaymentAppliedToObligation occurred:
                    return ApplyEvent(occurred);
                case AccountCreated occurred:
                    return ApplyEvent(occurred);
                case SuperSimpleSuperCoolDomainEventFoundByRules occurred:
                    return ApplyEvent(occurred);
                case TaxAppliedDuringBilling occurred:
                    return ApplyEvent(occurred);
                case UacAppliedAfterBilling occurred:
                    return ApplyEvent(occurred);
                case AccountBusinessRuleValidationSuccess occurred:
                    return ApplyEvent(occurred);


                default:
                    throw new UnknownAccountEventException($"{domainEvent.GetType()}");
            }
        }

        private AccountState ApplyEvent(AccountBusinessRuleValidationSuccess occurred)
        {
            return new AccountState(
                AccountNumber,
                CurrentBalance,
                AccountStatus,
                Obligations,
                SimulatedFields,
                AuditLog.Add(
                    new StateLog(
                        "AccountBusinessRuleValidationSuccess",
                        $"{occurred.Message}",
                        occurred.UniqueGuid(),
                        occurred.OccurredOn()
                    )
                ),
                OpeningBalance,
                Inventroy,
                UserName,
                LastPaymentAmount,
                LastPaymentDate
            );
        }

        private AccountState ApplyEvent(UacAppliedAfterBilling occurred)
        {
            var trans = new FinancialTransaction(new Tax {Amount = occurred.UacAmountApplied},
                occurred.UacAmountApplied);
            Obligations[occurred.ObligationNumber]?.PostTransaction(trans);
            var newBal = CurrentBalance + decimal.Parse((-1 * occurred.UacAmountApplied).ToString());
            var newState = new AccountState(
                AccountNumber,
                newBal,
                AccountStatus,
                Obligations,
                SimulatedFields,
                AuditLog.Add(new StateLog(
                    "UacAppliedAfterBilling", 
                    $"{occurred.Message} Balance After: {newBal:C}", 
                    occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                OpeningBalance,
                Inventroy,
                UserName,
                LastPaymentAmount,
                LastPaymentDate
            );


            return newState;
        }

        private AccountState ApplyEvent(TaxAppliedDuringBilling occurred)
        {
            var trans = new FinancialTransaction(new Tax {Amount = (double)occurred.TaxAmountApplied},
                (double)occurred.TaxAmountApplied);
            Obligations[occurred.ObligationNumber]?.PostTransaction(trans);
            var newState = new AccountState(
                AccountNumber,
                CurrentBalance + occurred.TaxAmountApplied,
                AccountStatus,
                Obligations,
                SimulatedFields,
                AuditLog.Add(new StateLog(
                    "TaxAppliedDuringBilling", 
                    occurred.Message  + " Balance After: " + (CurrentBalance + occurred.TaxAmountApplied).ToString("C"), 
                    occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                OpeningBalance,
                Inventroy,
                UserName,
                LastPaymentAmount,
                LastPaymentDate);

            return newState;
        }

       
        private AccountState ApplyEvent(SomeOneSaidHiToMe occurred)
        {
            return new AccountState(
                AccountNumber,
                CurrentBalance,
                AccountStatus,
                Obligations,
                LoadSimulation().ToImmutableDictionary(),
                AuditLog.Add(new StateLog("SomeOneSaidHiToMe", occurred.Message, occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                OpeningBalance,
                Inventroy,
                UserName,
                LastPaymentAmount,
                LastPaymentDate);
        }

        private AccountState ApplyEvent(SuperSimpleSuperCoolDomainEventFoundByRules occurred)
        {
            return new AccountState(
                AccountNumber,
                CurrentBalance,
                AccountStatus, Obligations,
                LoadSumulation(SimulatedFields, "1", "My state has been updated, see..."),
                AuditLog.Add(new StateLog("SuperSimpleSuperCoolEventFoundByRules", occurred.Message,
                    occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                OpeningBalance,
                Inventroy,
                UserName,
                LastPaymentAmount,
                LastPaymentDate);
        }

        private AccountState ApplyEvent(ObligationAssessedConcept occurred)
        {
            if (!Obligations.ContainsKey(occurred.ObligationNumber))
            {
                Console.WriteLine(
                    $"Account {AccountNumber} does not contain obligation {occurred.ObligationNumber}. No Action Taken.");
                throw new Exception(
                    $"Account {AccountNumber} does not contain obligation {occurred.ObligationNumber}. No Action Taken.");
            }

            var trans = new FinancialTransaction(occurred.FinancialBucket, occurred.FinancialBucket.Amount);
            Obligations[occurred.ObligationNumber].PostTransaction(trans);
            var newBalance = CurrentBalance + decimal.Parse(occurred.FinancialBucket.Amount.ToString());
            var newState = new AccountState(
                AccountNumber,
                newBalance,
                AccountStatus,
                Obligations,
                SimulatedFields,
                AuditLog.Add(
                    new StateLog(
                        "ObligationAssessedConcept",
                        occurred.Message + " Balance After: " + ((decimal) newBalance).ToString("C"),
                        occurred.UniqueGuid(),
                        occurred.OccurredOn())
                ),
                OpeningBalance,
                Inventroy,
                UserName,
                LastPaymentAmount,
                LastPaymentDate);
            return newState;
        }

        private AccountState ApplyEvent(AccountCurrentBalanceUpdated occurred)
        {
            var adjustmentsObligation = new MaintenanceFee("AccountAdjustments", 0, ObligationStatus.Boarding);
            
            if (Obligations.ContainsKey("AccountAdjustments"))
            {
                adjustmentsObligation = Obligations["AccountAdjustments"];
            }

            var trans = new FinancialTransaction(new Adjustment(occurred.CurrentBalance), occurred.CurrentBalance);

            adjustmentsObligation.PostTransaction(trans);
 
            
            return new AccountState(
                AccountNumber,
                decimal.Parse(occurred.CurrentBalance.ToString()),
                AccountStatus,
                Obligations.Add(adjustmentsObligation.ObligationNumber, adjustmentsObligation),
                SimulatedFields,
                AuditLog.Add(new StateLog("AccountCurrentBalanceUpdated", "Boarding Adjustment", occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                OpeningBalance,
                Inventroy,
                UserName,
                LastPaymentAmount,
                LastPaymentDate);
        }

        private AccountState ApplyEvent(AccountStatusChanged occurred)
        {
            return new AccountState(
                AccountNumber,
                CurrentBalance,
                occurred.AccountStatus,
                Obligations,
                SimulatedFields,
                AuditLog.Add(new StateLog("AccountStatusChanged", occurred.Message, occurred.UniqueGuid(),
                    occurred.OccurredOn())),
                OpeningBalance,
                Inventroy,
                UserName,
                LastPaymentAmount,
                LastPaymentDate);
        }

        private AccountState ApplyEvent(AccountCancelled occurred)
        {
            return new AccountState(AccountNumber,
                CurrentBalance,
                occurred.AccountStatus, Obligations,
                SimulatedFields,
                AuditLog.Add(new StateLog("AccountCancelled", occurred.Message, occurred.UniqueGuid(),
                    occurred.OccurredOn()))
                , OpeningBalance,
                Inventroy,
                UserName,
                LastPaymentAmount,
                LastPaymentDate);
        }

        private AccountState ApplyEvent(ObligationAddedToAccount occurred)
        {
            return new AccountState(
                AccountNumber,
                CurrentBalance,
                AccountStatus,
                Obligations.Add(occurred.MaintenanceFee.ObligationNumber, occurred.MaintenanceFee),
                SimulatedFields,
                AuditLog.Add(new StateLog("ObligationAddedToAccount", occurred.Message, occurred.UniqueGuid(),
                    occurred.OccurredOn()))
                , OpeningBalance,
                Inventroy,
                UserName,
                LastPaymentAmount,
                LastPaymentDate);
        }

        private AccountState ApplyEvent(PaymentAppliedToObligation occurred)
        {
            var trans = new FinancialTransaction(occurred.FinancialBucket, occurred.Amount);
            Obligations[occurred.ObligationNumber].PostTransaction(trans);
            decimal newBal = CurrentBalance - decimal.Parse(occurred.Amount.ToString()); 
            return new AccountState(
                AccountNumber,
                newBal,
                AccountStatus,
                Obligations,
                SimulatedFields,
                AuditLog.Add(new StateLog(
                    "PaymentAppliedToObligation", 
                    $"{occurred.Message} Balance After: {newBal :C}", 
                    occurred.UniqueGuid(),
                    occurred.OccurredOn()))
                , OpeningBalance
                , Inventroy
                , UserName
                , lastPaymentAmount: occurred.Amount
                , lastPaymentDate: DateTime.Today);
        }

        private AccountState ApplyEvent(AccountCreated occurred)
        {
            var newState = new AccountState(
                occurred.AccountNumber,
                accountStatus: AccountStatus.Boarded,
                obligations: ImmutableDictionary.Create<string, MaintenanceFee>(),
                simulation: LoadSimulation(),
                log: AuditLog.Add(
                    new StateLog("AccountCreated",
                        occurred.Message + " Balance After: " + ((decimal)occurred.OpeningBalance).ToString("C"),
                        occurred.UniqueGuid(),
                        occurred.OccurredOn()
                    )
                ),
                openingBalance: occurred.OpeningBalance,
                currentBalance: 0, //best to affect currBal explicitly with an event
                inventory: occurred.Inventory,
                userName: occurred.UserName,
                lastPaymentAmount: occurred.LastPaymentAmount,
                lastPaymentDate: occurred.LastPaymentDate
            );

            return newState;
        }


        /* Helpers */
        private static ImmutableDictionary<string, string> LoadSimulation()
        {
            var range = ImmutableDictionary.Create<string, string>();
            for (var i = 1; i <= 10; i++)
                range = range.Add(i.ToString(), $"This simulates field {i}");

            return range;
        }

        private static ImmutableDictionary<string, string> LoadSumulation(ImmutableDictionary<string, string> state,
            string keyToUpdate,
            string valueToUpdate)
        {
            state = state.SetItem(keyToUpdate, valueToUpdate);
            return state;
        }


        public override string ToString()
        {
            return
                $"{nameof(UserName)}: {UserName}, {nameof(Inventroy)}: {Inventroy}, {nameof(OpeningBalance)}: {OpeningBalance}, {nameof(AccountNumber)}: {AccountNumber}, {nameof(CurrentBalance)}: {CurrentBalance}, {nameof(AccountStatus)}: {AccountStatus}, {nameof(AuditLog)}: {AuditLog}, {nameof(SimulatedFields)}: {SimulatedFields}, {nameof(Obligations)}: {Obligations}, {nameof(LastPaymentAmount)}: {LastPaymentAmount}, {nameof(LastPaymentDate)}: {LastPaymentDate}";
        }

        public object Clone()
        {
            var newDict = this.Obligations.ToDictionary(x => x.Key, y => (MaintenanceFee) y.Value.Clone());
             return new AccountState(
                  this.AccountNumber
                , this.CurrentBalance
                , this.AccountStatus
                , newDict.ToImmutableDictionary()
                , this.SimulatedFields.ToImmutableDictionary()
                , this.AuditLog.Select(x => x.Clone()).Cast<StateLog>().ToImmutableList()
                , this.OpeningBalance
                , this.Inventroy
                , this.UserName
                , this.LastPaymentAmount
                , this.LastPaymentDate );
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Rules
{
    public class BillingConceptCannotBeBilledMoreThanOnce : IAccountBusinessRule
    {
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated;

        public BillingConceptCannotBeBilledMoreThanOnce(
            (string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public BillingConceptCannotBeBilledMoreThanOnce(AccountState accountState)
        {
            AccountState = accountState;
        }

        private AccountState AccountState { get; set; }

        private (string Command, Dictionary<string, object> Parameters) CommandState { get; set; }


        public bool Success { get; private set; }

        /* Rule logic goes here. */
        public void RunRule(IDomainCommand command)
        {    switch (command)
            {
                case BillingAssessment billing:
                    RunRule(billing);
                    break;
                default:
                    throw new NotImplementedException();
            }
           
        }

        public void RunRule(BillingAssessment command)
        {
            var conceptName = command.LineItems.Select(x => x.Item.Name).ToDictionary(x => x, x => x);

//            Console.WriteLine($"@@@@Rule BillingConceptCannotBeBilledMoreThanOnce: {command.AccountNumber}  " +
//                              $"has Obligations: {AccountState.Obligations.Count} and matches=" +
//                              $"{AccountState.Obligations.Any(x => x.Value.Transactions.Any(y => conceptName.ContainsKey(y.FinancialBucket.Name)))}");
           
            var matches =
                AccountState.Obligations.Any(x =>
                    x.Value.Transactions.Any(y => conceptName.ContainsKey(y.FinancialBucket.Name)));
            if (matches) // if we get a hit we bail
            {
                _eventsGenerated = new List<IDomainEvent>
                {
                    new AccountBusinessRuleValidationFailure(
                        AccountState.AccountNumber,
                        "AccountBusinessRuleValidationFailure on BillingConceptCannotBeBilledMoreThanOnce"
                    )
                };
                _detailsGenerated = "THIS FAILED";
                Success = false;
                return;
            }

            _eventsGenerated = new List<IDomainEvent>
            {
                new AccountBusinessRuleValidationSuccess(
                    AccountState.AccountNumber,
                    "AccountBusinessRuleValidationSuccess on BillingConceptCannotBeBilledMoreThanOnce"
                )
            };
            _detailsGenerated = "THIS WORKED";
            Success = true;
        }

        public void SetAccountState(AccountState state)
        {
            AccountState = state;
        }

        public void SetCallingCommandState((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public string GetResultDetails()
        {
            return _detailsGenerated;
        }

        public List<IDomainEvent> GetGeneratedEvents()
        {
            return _eventsGenerated;
        }

        public AccountState GetGeneratedState()
        {
            return AccountState;
        }

        public bool RuleAppliedSuccessfuly()
        {
            return Success;
        }
    }
}
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Rules
{
    public class ClientSpecificRuleForCalculatingTax : IAccountBusinessRule
    {
        /* Rule logic goes here. */
        public void RunRule(IDomainCommand command)
        {
            //Extract parameter Dues from Command
            double duesAmount = 0.00;
            var com = (BillingAssessment) command;
            foreach (var c in com.LineItems)
            {
                if (c.Item.Name.Equals("Dues"))
                {
                    duesAmount = c.Item.Amount;
                    break;
                }
            }
            if (duesAmount != 0.00)
            {
                _eventsGenerated = new List<IDomainEvent>
                {
                    new AccountBusinessRuleValidationFailure(
                        AccountState.AccountNumber,
                        "ClientSpecificRuleForCalculatingTax requires a 'Dues' amount be provided when billing."
                    )
                };
                ;
                Success = false;
                return;
            }

            _eventsGenerated = new List<IDomainEvent>
            {
                new TaxAppliedDuringBilling(
                    AccountState.AccountNumber,
                    AccountState.Obligations.FirstOrDefault(x => x.Value.Status == ObligationStatus.Active).Key,
                    (15.0 / 100) * duesAmount
                )
            };
            _detailsGenerated = "THIS WORKED";
            Success = true;
        }

        private AccountState AccountState { get; set; }
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated;

        private (string Command, Dictionary<string, object> Parameters) CommandState { get; set; }

        public ClientSpecificRuleForCalculatingTax((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public ClientSpecificRuleForCalculatingTax(AccountState accountState)
        {
            AccountState = accountState;
        }

        public void SetAccountState(AccountState state)
        {
            AccountState = state;
        }

        public void SetCallingCommandState((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public bool Success { get; private set; }

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
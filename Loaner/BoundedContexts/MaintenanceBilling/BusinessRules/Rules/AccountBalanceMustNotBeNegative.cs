using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.Commands;
using Loaner.BoundedContexts.MaintenanceBilling.Events;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Rules
{
    public class AccountBalanceMustNotBeNegative : IAccountBusinessRule
    {
        private AccountState AccountState { get; set; }
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated;
        private (string Command, Dictionary<string, object> Parameters) CommandState { get; set; }

        /* Rule logic goes here. */
        public void RunRule(IDomainCommand command)
        {
            //User CommandState to get list of passed in options. 
            // and COMMAND to merge the specifics of the command

            _eventsGenerated = new List<IDomainEvent>
            {
                new AccountBusinessRuleValidationSuccess(
                    AccountState.AccountNumber,
                    "AccountBalanceMustNotBeNegative"
                )
            };
            _detailsGenerated = "THIS WORKED";
            Success = true;
        }

        public AccountBalanceMustNotBeNegative((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public AccountBalanceMustNotBeNegative(AccountState accountState)
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
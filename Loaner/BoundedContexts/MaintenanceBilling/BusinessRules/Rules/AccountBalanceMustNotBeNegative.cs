using System;
using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Rules
{
    public class AccountBalanceMustNotBeNegative : IAccountBusinessRule
    {
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated;

        public AccountBalanceMustNotBeNegative((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public AccountBalanceMustNotBeNegative(AccountState accountState)
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

            //User CommandState to get list of passed in options. 
            // and COMMAND to merge the specifics of the command

            _eventsGenerated = new List<IDomainEvent>
            {
                new AccountBusinessRuleValidationSuccess(
                    AccountState.AccountNumber,
                    "AccountBalanceMustNotBeNegative"
                )
            };
            _detailsGenerated = "THIS WILL ALWAYS WORK FOR THE DEMO";
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
using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.StateModels;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.Events;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Rules
{
    public class AccountBalanceMustNotBeNegative : IAccountBusinessRule
    {
        private AccountState AccountState { get; set; }
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated;

        public AccountBalanceMustNotBeNegative()
        {
        }

        public AccountBalanceMustNotBeNegative(AccountState accountState)
        {
            AccountState = accountState;
        }

        public void SetAccountState(AccountState state)
        {
            AccountState = state;
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


        /* Rule logic goes here. */
        public void RunRule()
        {
            _eventsGenerated = new List<IDomainEvent>
            {
                new SuperSimpleSuperCoolDomainEventFoundByRules(
                    AccountState.AccountNumber,
                    "AccountBalanceMustNotBeNegative"
                )
            };
            _detailsGenerated = "THIS WORKED";
            Success = true;
        }
    }
}
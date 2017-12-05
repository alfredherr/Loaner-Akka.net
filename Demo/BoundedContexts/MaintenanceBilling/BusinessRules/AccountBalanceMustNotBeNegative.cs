using System.Collections.Generic;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates.StateModels;
using Demo.BoundedContexts.MaintenanceBilling.Events;

namespace Demo.BoundedContexts.MaintenanceBilling.BusinessRules
{
    public class AccountBalanceMustNotBeNegative : IAccountBusinessRule
    {
        private readonly AccountState _accountState;
        private string _detailsGenerated;
        private List<IEvent> _eventsGenerated;

        public AccountBalanceMustNotBeNegative(AccountState accountState)
        {
            this._accountState = accountState;
        }

        public bool Success { get; private set; }

        public string GetResultDetails()
        {
            return _detailsGenerated;
        }

        public List<IEvent> GetGeneratedEvents()
        {
            return _eventsGenerated;
        }

        public AccountState GetGeneratedState()
        {
            return _accountState;
        }

        /* Rule logic goes here. */
        public void RunRule()
        {
            _eventsGenerated = new List<IEvent>
            {
                new SuperSimpleSuperCoolEventFoundByRules(
                    _accountState.AccountNumber,
                    "AccountBalanceMustNotBeNegative"
                )
            };
            _detailsGenerated = "THIS WORKED";
            Success = true;
        }
    }
}
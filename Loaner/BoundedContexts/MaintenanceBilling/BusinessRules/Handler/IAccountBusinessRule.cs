using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    public interface IAccountBusinessRule
    {
        /**
         * Rule logic gets handled in this method.
         */
        void RunRule(IDomainCommand command);

        string GetResultDetails();

        /** 
         * The purpose of a rule is to return a list of orderd events which must be 
         * applied to the account 
        */
        List<IDomainEvent> GetGeneratedEvents();

        /**
         * This is what the resulting AccountState would be from all the applied
         * events -- it can be used to allow comparisons, or for downstream rules
         * perhaps. Note that this its the calles choice to accept the events or not.
         */
        AccountState GetGeneratedState();

        bool RuleAppliedSuccessfuly();

        void SetAccountState(AccountState accountState);

        void SetCallingCommandState((string Command, Dictionary<string, object> Parameters) commandState);
    }
}
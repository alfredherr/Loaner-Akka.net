using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.StateModels;
using Loaner.BoundedContexts.MaintenanceBilling.Events;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    public class BusinessRuleApplicationResult
    {
        public BusinessRuleApplicationResult()
        {
            RuleProcessedResults = new Dictionary<IAccountBusinessRule, string>();
            GeneratedEvents = new List<IDomainEvent>();
            GeneratedState = new AccountState();
            Success = false;
        }

        public bool Success { get; set; }
        public AccountState GeneratedState { get; set; }
        public List<IDomainEvent> GeneratedEvents { get; set; }
        public Dictionary<IAccountBusinessRule, string> RuleProcessedResults { get; set; }
    }
}
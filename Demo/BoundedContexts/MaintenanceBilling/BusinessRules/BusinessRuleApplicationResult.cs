using System.Collections.Generic;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates.StateModels;
using Demo.BoundedContexts.MaintenanceBilling.Events;

namespace Demo.BoundedContexts.MaintenanceBilling.BusinessRules
{
    public class BusinessRuleApplicationResult
    {
        public BusinessRuleApplicationResult()
        {
            RuleProcessedResults = new Dictionary<IAccountBusinessRule, string>();
            GeneratedEvents = new List<IEvent>();
            GeneratedState = new AccountState();
            Success = false;
        }

        public bool Success { get; set; }
        public AccountState GeneratedState { get; set; }
        public List<IEvent> GeneratedEvents { get; set; }
        public Dictionary<IAccountBusinessRule, string> RuleProcessedResults { get; set; }
    }
}
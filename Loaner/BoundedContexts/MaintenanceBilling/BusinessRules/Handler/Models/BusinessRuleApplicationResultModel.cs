using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models
{
    public class BusinessRuleApplicationResultModel
    {
        public BusinessRuleApplicationResultModel()
        {
            RuleProcessedResults = new Dictionary<IAccountBusinessRule, string>();
            GeneratedEvents = new List<IDomainEvent>();
            GeneratedState = new AccountState();
            Success = false; 
        }

        public double TotalBilledAmount { get; set; } //TODO move this out of here. Needmore thoughtout solution
        public bool Success { get; set; }
        public AccountState GeneratedState { get; set; }
        public List<IDomainEvent> GeneratedEvents { get; set; }
        public Dictionary<IAccountBusinessRule, string> RuleProcessedResults { get; set; }
    }
}
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    public class GetBusinessRulesToApply
    {
        public GetBusinessRulesToApply(ApplyBusinessRules applyBusinessRules)
        {
            ApplyBusinessRules = applyBusinessRules;
        }

        public ApplyBusinessRules ApplyBusinessRules { get; set; }
    }
}
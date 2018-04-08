using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    public class GetBusinessRulesToApply
    {
        public GetBusinessRulesToApply(FetchAccountBusinessRules fetchAccountBusinessRules)
        {
            FetchAccountBusinessRules = fetchAccountBusinessRules;
        }

        public FetchAccountBusinessRules FetchAccountBusinessRules { get; set; }
    }
}
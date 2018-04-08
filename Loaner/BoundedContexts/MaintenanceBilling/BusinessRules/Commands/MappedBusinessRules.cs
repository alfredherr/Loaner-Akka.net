using System.Collections.Generic;
using System.Linq;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    public class MappedBusinessRules
    {
        public MappedBusinessRules(FetchAccountBusinessRules fetchAccountBusinessRules, List<IAccountBusinessRule> rules)
        {
            FetchAccountBusinessRules = fetchAccountBusinessRules;
            Rules = rules;
        }

        public FetchAccountBusinessRules FetchAccountBusinessRules { get; set; }
        
        public List<IAccountBusinessRule> Rules { get; set; }
    }
}
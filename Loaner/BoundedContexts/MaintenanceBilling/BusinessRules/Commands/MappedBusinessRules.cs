using System.Collections.Generic;
using System.Linq;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    public class MappedBusinessRules
    {
        public MappedBusinessRules(ApplyBusinessRules applyBusinessRules, List<IAccountBusinessRule> rules)
        {
            ApplyBusinessRules = applyBusinessRules;
            Rules = rules;
        }

        public ApplyBusinessRules ApplyBusinessRules { get; set; }
        
        public List<IAccountBusinessRule> Rules { get; set; }
    }
}
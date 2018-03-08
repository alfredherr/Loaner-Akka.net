using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    public class UpdateAccountBusinessRules
    {
        public UpdateAccountBusinessRules(List<AccountBusinessRuleMapModel> updatedRules)
        {
            UpdatedRules = updatedRules;
        }
        public List<AccountBusinessRuleMapModel> UpdatedRules { get; }
    }
}
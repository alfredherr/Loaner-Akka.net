using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models;

namespace Loaner.API.Models
{
    internal class BusinessRulesMapModel
    {
        public List<AccountBusinessRuleMapModel> RulesMap { get; set; }
        public string Message { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;

namespace Loaner.API.Models
{
    class BusinessRulesMapModel
    {
       public List<AccountBusinessRuleMap> RulesMap { get; set; }
        public string Message { get; set; }
    }
}

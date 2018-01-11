using System;
using System.Collections.Generic;
using System.Text;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models;

namespace Loaner.API.Models
{
    class BusinessRulesMapModel
    {
       public List<AccountBusinessRuleMapModel> RulesMap { get; set; }
        public string Message { get; set; }
    }
}

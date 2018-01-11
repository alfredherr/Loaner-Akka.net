using System.Collections.Generic;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models
{
    public class CommandToBusinessRuleModel
    {
        public string Command { get; set; }
        public string BusinessRule { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public string Description { get; set; }
    }
}
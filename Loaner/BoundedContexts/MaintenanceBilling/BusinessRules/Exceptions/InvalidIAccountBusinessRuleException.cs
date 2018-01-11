using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Exceptions
{
    public class InvalidIAccountBusinessRuleException : Exception
    {
        public InvalidIAccountBusinessRuleException(string rule) : base(rule)
        {
        }
    }
}
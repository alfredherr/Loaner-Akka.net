using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Exceptions
{
    public class InvalidBusinessRulesMapFileException : Exception
    {
        public InvalidBusinessRulesMapFileException(string businessRulesMapFile) : base(businessRulesMapFile)
        {
        }
    }
}
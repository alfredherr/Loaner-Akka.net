using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Exceptions
{
    public class CommandStateOptionMissingException : Exception
    {
        public CommandStateOptionMissingException(string s) : base(s)
        {
        }
    }
}
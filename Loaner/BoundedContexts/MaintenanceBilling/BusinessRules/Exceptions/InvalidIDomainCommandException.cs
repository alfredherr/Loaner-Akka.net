using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Exceptions
{
    public class InvalidIDomainCommandException : Exception
    {
        public InvalidIDomainCommandException(string command) : base(command)
        {
        }
    }
}
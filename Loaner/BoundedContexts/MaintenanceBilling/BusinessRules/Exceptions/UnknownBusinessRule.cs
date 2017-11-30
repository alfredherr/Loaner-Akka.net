using System;
using System.Runtime.Serialization;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Exceptions
{
    internal class UnknownBusinessRule : Exception
    {
        public UnknownBusinessRule()
        {
        }

        public UnknownBusinessRule(string message) : base(message)
        {
        }

        public UnknownBusinessRule(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnknownBusinessRule(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
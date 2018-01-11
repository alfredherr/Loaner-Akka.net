using System;
using System.Runtime.Serialization;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Exceptions
{
    internal class UnknownBusinessRuleException : Exception
    {
        public UnknownBusinessRuleException()
        {
        }

        public UnknownBusinessRuleException(string message) : base(message)
        {
        }

        public UnknownBusinessRuleException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnknownBusinessRuleException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
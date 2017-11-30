using System;
using System.Runtime.Serialization;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Exceptions
{
    internal class UnknownAccountEvent : Exception
    {
        public UnknownAccountEvent()
        {
        }

        public UnknownAccountEvent(string message) : base(message)
        {
        }

        public UnknownAccountEvent(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnknownAccountEvent(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
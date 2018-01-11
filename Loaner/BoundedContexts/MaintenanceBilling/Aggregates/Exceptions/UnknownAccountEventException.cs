using System;
using System.Runtime.Serialization;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Exceptions
{
    internal class UnknownAccountEventException : Exception
    {
        public UnknownAccountEventException()
        {
        }

        public UnknownAccountEventException(string message) : base(message)
        {
        }

        public UnknownAccountEventException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnknownAccountEventException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
using System;
using System.Runtime.Serialization;

namespace Loaner.SnapShotStore3
{
    internal class ObjectDoesNotImplementICloneable : Exception
    {
        public ObjectDoesNotImplementICloneable()
        {
        }

        public ObjectDoesNotImplementICloneable(string message) : base(message)
        {
        }

        public ObjectDoesNotImplementICloneable(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ObjectDoesNotImplementICloneable(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
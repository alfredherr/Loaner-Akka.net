using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.Events
{
    public interface IEvent
    {
        DateTime OccurredOn();
        Guid UniqueGuid();
    }
}
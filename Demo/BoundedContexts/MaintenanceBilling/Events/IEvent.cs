using System;

namespace Demo.BoundedContexts.MaintenanceBilling.Events
{
    public interface IEvent
    {
        DateTime OccurredOn();
        Guid UniqueGuid();
    }
}
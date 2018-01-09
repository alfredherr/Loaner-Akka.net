using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.Events
{
    public interface IDomainEvent
    {
        DateTime OccurredOn();
        Guid UniqueGuid();
    }
}
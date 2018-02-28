using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public interface IDomainEvent
    {
        DateTime OccurredOn();
        Guid UniqueGuid();
    }
}
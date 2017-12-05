using System;

namespace Demo.BoundedContexts.MaintenanceBilling.Commands
{
    public interface IDomainCommand
    {
        DateTime RequestedOn();
        Guid UniqueGuid();
    }
}
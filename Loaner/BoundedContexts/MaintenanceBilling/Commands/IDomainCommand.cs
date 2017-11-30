using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.Commands
{
    public interface IDomainCommand
    {
        DateTime RequestedOn();
        Guid UniqueGuid();
    }
}
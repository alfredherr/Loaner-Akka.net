using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainCommands
{
    public interface IDomainCommand
    {
        DateTime RequestedOn();
        Guid UniqueGuid();
    }
}
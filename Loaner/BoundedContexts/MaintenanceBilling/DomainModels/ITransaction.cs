using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public interface ITransaction
    {
        DateTime OccurredOn();
        Guid UniqueGuid();
    }
}
using System;

namespace Demo.BoundedContexts.MaintenanceBilling.Models
{
    public interface ITransaction
    {
        DateTime OccurredOn();
        Guid UniqueGuid();
    }
}
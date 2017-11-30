using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.Models
{
    public interface ITransaction
    {
        DateTime OccurredOn();
        Guid UniqueGuid();
    }
}
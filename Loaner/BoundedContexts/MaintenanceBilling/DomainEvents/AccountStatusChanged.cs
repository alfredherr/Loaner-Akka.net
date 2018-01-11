using System;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public class AccountStatusChanged : IDomainEvent
    {
        public AccountStatusChanged()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public AccountStatusChanged(string accountNumber, AccountStatus status) : this()
        {
            AccountStatus = status;
            AccountNumber = accountNumber;
        }

        public AccountStatus AccountStatus { get; }
        public string AccountNumber { get; }
        private DateTime _OccurredOn { get; }
        private Guid _UniqueGuid { get; }

        public DateTime OccurredOn()
        {
            return _OccurredOn;
        }

        public Guid UniqueGuid()
        {
            return _UniqueGuid;
        }
    }
}
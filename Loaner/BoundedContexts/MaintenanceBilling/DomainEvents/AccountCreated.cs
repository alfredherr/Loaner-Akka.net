using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public class AccountCreated : IDomainEvent
    {
        public AccountCreated(string accountNumber, string message = "")
        {
            AccountNumber = accountNumber;
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
            Message = message;
        }
        public string Message { get;  }
        private DateTime _OccurredOn { get; }
        public string AccountNumber { get; }
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
using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    internal class AccountAddedToSupervision : IDomainEvent
    {
        public AccountAddedToSupervision()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public AccountAddedToSupervision(string accountNumber,decimal currentAccountBalance, string message = "") : this()
        {
            CurrentBalance = currentAccountBalance;
            Message = message;
            AccountNumber = accountNumber;
        }

        public decimal CurrentBalance { get; set; }
        public string AccountNumber { get; set; }
        public string Message { get; set; }
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
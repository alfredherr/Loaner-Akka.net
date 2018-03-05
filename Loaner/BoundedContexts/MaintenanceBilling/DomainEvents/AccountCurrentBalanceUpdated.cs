using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public class AccountCurrentBalanceUpdated : IDomainEvent
    {
        public AccountCurrentBalanceUpdated(string accountNumber, double newCurrentBalance, string message = "") :
            this()
        {
            Message = message;

            AccountNumber = accountNumber;
            CurrentBalance = newCurrentBalance;
        }

        public AccountCurrentBalanceUpdated()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public string Message { get; set; }

        public string AccountNumber { get; }

        public double CurrentBalance { get; }

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
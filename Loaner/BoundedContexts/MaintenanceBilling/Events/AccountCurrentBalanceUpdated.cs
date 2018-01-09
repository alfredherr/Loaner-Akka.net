using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.Events
{
    public class AccountCurrentBalanceUpdated : IDomainEvent
    {
        public AccountCurrentBalanceUpdated(string accountNumber, double newCurrentBalance) : this()
        {
            AccountNumber = accountNumber;
            CurrentBalance = newCurrentBalance;
        }

        public AccountCurrentBalanceUpdated()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

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
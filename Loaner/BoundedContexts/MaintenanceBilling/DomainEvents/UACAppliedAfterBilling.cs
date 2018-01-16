using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public class UacAppliedAfterBilling : IDomainEvent
    {
        public UacAppliedAfterBilling(string accountNumber, string obligationNumber, double uacAmount)
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
            UacAmountApplied = uacAmount;
            AccountNumber = accountNumber;
            ObligationNumber = obligationNumber;
        }

        public string ObligationNumber { get; }
        public string AccountNumber { get; }
        private DateTime _OccurredOn { get; }
        private Guid _UniqueGuid { get; }
        public double UacAmountApplied { get; }

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
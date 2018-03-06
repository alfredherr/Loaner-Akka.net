using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public class TaxAppliedDuringBilling : IDomainEvent
    {
        public TaxAppliedDuringBilling()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public TaxAppliedDuringBilling(decimal taxAmount) : this()
        {
            TaxAmountApplied = taxAmount;
        }

        public TaxAppliedDuringBilling(string accountNumber, string obligationNumber, decimal taxAmount,
            string message = "") : this()
        {
            TaxAmountApplied = taxAmount;
            AccountNumber = accountNumber;
            ObligationNumber = obligationNumber;
            Message = message;
        }

        public string Message { get; set; }
        public string AccountNumber { get; }
        public string ObligationNumber { get; }
        private DateTime _OccurredOn { get; }
        private Guid _UniqueGuid { get; }
        public decimal TaxAmountApplied { get; }

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
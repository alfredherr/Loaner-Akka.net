using System;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public class ObligationSettledConcept : IDomainEvent
    {
        public ObligationSettledConcept()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public ObligationSettledConcept(string obligationNumber, IFinancialBucket bucket, double amount,
            string message = "") : this()
        {
            ObligationNumber = obligationNumber;
            FinancialBucket = bucket;
            Amount = amount;
            Message = message;
        }

        public string Message { get; set; }

        public string ObligationNumber { get; }
        public IFinancialBucket FinancialBucket { get; }
        public double Amount { get; }
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
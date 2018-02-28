using System;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public class ObligationAssessedConcept : IDomainEvent
    {
        public ObligationAssessedConcept()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public ObligationAssessedConcept(string obligationNumber, IFinancialBucket bucket, string message = "") : this()
        {
            ObligationNumber = obligationNumber;
            FinancialBucket = bucket;
            Message = message;
        }

        public string Message { get; set; }

        public string ObligationNumber { get; }
        public IFinancialBucket FinancialBucket { get; }

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

        public override string ToString()
        {
            return $"[ObligationNumber={ObligationNumber}, FinancialBucket={FinancialBucket.GetType().Name}]";
        }
    }
}
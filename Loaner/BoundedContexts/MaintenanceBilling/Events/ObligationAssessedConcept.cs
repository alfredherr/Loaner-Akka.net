using System;
using Loaner.BoundedContexts.MaintenanceBilling.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.Events
{
    public class ObligationAssessedConcept : IEvent
    {
        public ObligationAssessedConcept()
        {
            
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }
        public ObligationAssessedConcept(string obligationNumber, IFinancialBucket bucket, double amount ) :this()  
        {
            ObligationNumber = obligationNumber;
            FinancialBucket = bucket;
            Amount = amount;            
        }

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

        public override string ToString()
        {
            return $"[ObligationNumber={ObligationNumber}, FinancialBucket={typeof(IFinancialBucket)},Amount={Amount}]";
        }
    }
}
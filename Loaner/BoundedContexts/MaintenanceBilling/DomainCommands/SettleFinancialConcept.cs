using System;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainCommands
{
    public class SettleFinancialConcept : IDomainCommand
    {
        public SettleFinancialConcept()
        {
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
        }

        public SettleFinancialConcept(string obligationNumber, IFinancialBucket bucket, double amount) : this()
        {
            ObligationNumber = obligationNumber;
            FinancialBucket = bucket;
            Amount = amount;
        }

        public string ObligationNumber { get; }
        public double Amount { get; }
        public IFinancialBucket FinancialBucket { get; }

        private DateTime _RequestedOn { get; }
        private Guid _UniqueGuid { get; }

        public DateTime RequestedOn()
        {
            return _RequestedOn;
        }

        public Guid UniqueGuid()
        {
            return _UniqueGuid;
        }
    }
}
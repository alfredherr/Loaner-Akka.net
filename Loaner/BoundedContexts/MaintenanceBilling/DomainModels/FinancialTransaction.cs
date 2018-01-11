using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class FinancialTransaction : ITransaction
    {
        public FinancialTransaction()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public FinancialTransaction(IFinancialBucket bucket, double amount) : this()
        {
            FinancialBucket = bucket;
            TransactionAmount = amount;
        }

        public double TransactionAmount { get; }
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
    }
}
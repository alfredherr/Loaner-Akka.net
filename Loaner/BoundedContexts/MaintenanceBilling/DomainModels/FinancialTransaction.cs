using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class FinancialTransaction : ITransaction, ICloneable
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
        private FinancialTransaction(IFinancialBucket bucket, double amount, DateTime _occurredOn, Guid _uniqueGuid) 
        {
            FinancialBucket = bucket;
            TransactionAmount = amount;
            _OccurredOn = _occurredOn;
            _UniqueGuid = _uniqueGuid;
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

        public object Clone()
        {
            return new FinancialTransaction(FinancialBucket, TransactionAmount, _OccurredOn, _UniqueGuid);
        }
    }
}
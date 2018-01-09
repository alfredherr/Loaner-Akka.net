using System;
using System.Collections.Generic;
using System.Text;
 

namespace Loaner.BoundedContexts.MaintenanceBilling.Events
{
    public class TaxAppliedDuringBilling : IDomainEvent
    {
        public TaxAppliedDuringBilling()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public TaxAppliedDuringBilling(double taxAmount) : this()
        {
            TaxAmountApplied = taxAmount;
        }

        public TaxAppliedDuringBilling(string accountNumber, double taxAmount) : this()
        {
            TaxAmountApplied = taxAmount;
            AccountNumber = accountNumber;
        }

        public string AccountNumber { get; }
        private DateTime _OccurredOn { get; }
        private Guid _UniqueGuid { get; }
        public double TaxAmountApplied { get; }

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
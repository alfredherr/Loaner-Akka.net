using System;
using System.Collections.Generic;
using System.Text;
 

namespace Loaner.BoundedContexts.MaintenanceBilling.Events
{
    public class UacAppliedAfterBilling : IDomainEvent
    {
        public UacAppliedAfterBilling()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public UacAppliedAfterBilling(double uacAmount) : this()
        {
            UacAmountApplied = uacAmount;
        }

        public UacAppliedAfterBilling(string accountNumber, double uacAmount) : this()
        {
            UacAmountApplied = uacAmount;
            AccountNumber = accountNumber;
        }

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
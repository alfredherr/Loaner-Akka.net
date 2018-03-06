using System;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainCommands
{
    public class PayAccount : IDomainCommand
    {
        public PayAccount(string accountNumber, double amountToPay)
        {
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
            AccountNumber = accountNumber;
            AmountToPay = amountToPay;
        }

        public string AccountNumber { get; }
        public double AmountToPay { get; }
        private Guid _UniqueGuid { get; }
        private DateTime _RequestedOn { get; }

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
using System;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public class ObligationAddedToAccount : IDomainEvent
    {
        public ObligationAddedToAccount(string accountNumber, MaintenanceFee maintenanceFee, string message = "")
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
            MaintenanceFee = maintenanceFee;
            AccountNumber = accountNumber;
            Message = message;
        }

        public MaintenanceFee MaintenanceFee { get; }
        public string AccountNumber { get; }
        private DateTime _OccurredOn { get; }
        private Guid _UniqueGuid { get; }
        public string Message { get; set; }

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
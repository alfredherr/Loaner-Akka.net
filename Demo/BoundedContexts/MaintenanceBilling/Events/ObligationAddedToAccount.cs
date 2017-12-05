using System;
using Demo.BoundedContexts.MaintenanceBilling.Models;

namespace Demo.BoundedContexts.MaintenanceBilling.Events
{
    public class ObligationAddedToAccount : IEvent
    {
        
        public ObligationAddedToAccount(string accountNumber, MaintenanceFee maintenanceFee)  
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
            MaintenanceFee = maintenanceFee;
            AccountNumber = accountNumber;
        }

        public MaintenanceFee MaintenanceFee { get; }
        public string AccountNumber { get; }
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
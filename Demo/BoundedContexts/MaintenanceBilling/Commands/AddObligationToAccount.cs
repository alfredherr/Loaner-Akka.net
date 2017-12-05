using System;
using Demo.BoundedContexts.MaintenanceBilling.Models;

namespace Demo.BoundedContexts.MaintenanceBilling.Commands
{
    public class AddObligationToAccount : IDomainCommand
    {
      
        public AddObligationToAccount(string accountNumber, MaintenanceFee maintenanceFee)  
        {
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
            AccountNumber = accountNumber;
            MaintenanceFee = maintenanceFee;
        }

        public string AccountNumber { get; }
        public MaintenanceFee MaintenanceFee { get; }
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
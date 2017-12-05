using System;

namespace Demo.BoundedContexts.MaintenanceBilling.Commands
{
    public class CreateAccount : IDomainCommand
    {
        public CreateAccount(string accountNumber)
        {
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
            AccountNumber = accountNumber;
        }

        public string AccountNumber { get; }
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
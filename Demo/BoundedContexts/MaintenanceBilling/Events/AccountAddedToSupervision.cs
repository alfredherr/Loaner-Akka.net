using System;

namespace Demo.BoundedContexts.MaintenanceBilling.Events
{
    internal class AccountAddedToSupervision : IEvent
    {
        public AccountAddedToSupervision()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
            
        }

        public AccountAddedToSupervision(string accountNumber) : this()
        {
            AccountNumber = accountNumber;
        }

        public string AccountNumber { get; set; }
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
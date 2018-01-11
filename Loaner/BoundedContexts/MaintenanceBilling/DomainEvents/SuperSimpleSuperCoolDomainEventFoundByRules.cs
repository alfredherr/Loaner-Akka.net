using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public class SuperSimpleSuperCoolDomainEventFoundByRules : IDomainEvent
    {
        public SuperSimpleSuperCoolDomainEventFoundByRules()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public SuperSimpleSuperCoolDomainEventFoundByRules(string message) : this()
        {
            Message = message;
        }

        public SuperSimpleSuperCoolDomainEventFoundByRules(string accountNumber, string message) : this()
        {
            Message = message;
            AccountNumber = accountNumber;
        }

        public string AccountNumber { get; }
        private DateTime _OccurredOn { get; }
        private Guid _UniqueGuid { get; }
        public string Message { get; }

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
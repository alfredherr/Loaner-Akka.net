using System;

namespace Demo.BoundedContexts.MaintenanceBilling.Events
{
    public class SuperSimpleSuperCoolEventFoundByRules : IEvent
    {
        public SuperSimpleSuperCoolEventFoundByRules()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public SuperSimpleSuperCoolEventFoundByRules(string message) : this()
        {
            Message = message;
        }

        public SuperSimpleSuperCoolEventFoundByRules(string accountNumber, string message) : this()
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
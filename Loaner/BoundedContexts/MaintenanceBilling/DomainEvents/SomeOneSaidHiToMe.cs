using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public class SomeOneSaidHiToMe : IDomainEvent
    {
        public SomeOneSaidHiToMe(string accountNumber, string debuInfo, string message = "")
        {
            AccountNumber = accountNumber;
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
            DebugInfo = debuInfo;
            Message = message;
        }

        public string Message { get; set; }
        private DateTime _OccurredOn { get; }
        public string AccountNumber { get; }
        private Guid _UniqueGuid { get; }

        public string DebugInfo { get; }

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
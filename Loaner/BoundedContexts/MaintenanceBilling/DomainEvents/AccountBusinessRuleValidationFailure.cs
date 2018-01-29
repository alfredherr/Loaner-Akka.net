using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public class AccountBusinessRuleValidationFailure : IDomainEvent
    {
        public AccountBusinessRuleValidationFailure()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public AccountBusinessRuleValidationFailure(string message) : this()
        {
            Message = message;
        }

        public AccountBusinessRuleValidationFailure(string accountNumber, string message = "") : this()
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
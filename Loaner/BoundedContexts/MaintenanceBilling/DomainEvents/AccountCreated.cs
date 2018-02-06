using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    public class AccountCreated : IDomainEvent
    {
        public AccountCreated(DateTime lastPaymentDate, double lastPaymentAmount, string accountNumber, double openingBalance, string inventory, string userName, string message = "")
        {
            AccountNumber = accountNumber;
            OpeningBalance = openingBalance;
            Inventory = inventory;
            UserName = userName;
            LastPaymentDate = lastPaymentDate;
            LastPaymentAmount = lastPaymentAmount;
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
            Message = message;
        }
        public double LastPaymentAmount { get; }
        public DateTime LastPaymentDate { get; }

        public double OpeningBalance { get; }
        public string Inventory { get; }
        public string UserName { get; }
        
        public string Message { get;  }
        private DateTime _OccurredOn { get; }
        public string AccountNumber { get; }
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
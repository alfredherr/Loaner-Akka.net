using System;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainCommands
{
    public class CreateAccount : IDomainCommand
    {
        public CreateAccount(string accountNumber, AccountBoardingModel boardingModel)
        {
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
            AccountNumber = accountNumber;
            BoardingModel = boardingModel;
        }

        public AccountBoardingModel BoardingModel { get; }
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
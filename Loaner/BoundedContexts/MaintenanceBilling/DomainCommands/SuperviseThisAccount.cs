using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainCommands
{
    public class SuperviseThisAccount : IDomainCommand
    {
        public SuperviseThisAccount()
        {
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
        }

        public SuperviseThisAccount(string portfolio, string accountNumber, double currentBalance) : this()
        {
            AccountNumber = accountNumber;
            Portfolio = portfolio;
            CurrentAccountBalance = currentBalance;
        }

        public double CurrentAccountBalance { get; private set; }

        private Guid _UniqueGuid { get; }
        private DateTime _RequestedOn { get; }
        public string AccountNumber { get; }
        public string Portfolio { get; }


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
using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainCommands
{
    public class SuperviseThisPortfolio : IDomainCommand
    {
        public SuperviseThisPortfolio()
        {
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
        }

        public SuperviseThisPortfolio(string portfolioName) : this()
        {
            PortfolioName = portfolioName;
        }

        private Guid _UniqueGuid { get; }
        private DateTime _RequestedOn { get; }

        public string PortfolioName { get; }


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
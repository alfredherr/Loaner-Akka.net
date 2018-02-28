using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainEvents
{
    internal class PortfolioAddedToSupervision : IDomainEvent
    {
        public PortfolioAddedToSupervision()
        {
            _UniqueGuid = Guid.NewGuid();
            _OccurredOn = DateTime.Now;
        }

        public PortfolioAddedToSupervision(string portfolioName, string message = "") : this()
        {
            PortfolioNumber = portfolioName;
            Message = message;
        }

        public string Message { get; set; }
        public string PortfolioNumber { get; set; }
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
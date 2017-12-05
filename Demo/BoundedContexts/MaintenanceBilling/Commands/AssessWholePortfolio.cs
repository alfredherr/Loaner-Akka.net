using System;
using System.Collections.Generic;
using Demo.BoundedContexts.MaintenanceBilling.Models;

namespace Demo.BoundedContexts.MaintenanceBilling.Commands
{
    public class AssessWholePortfolio : IDomainCommand
    {
        public AssessWholePortfolio(string portfolioName, List<InvoiceLineItem> items)
        {
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
            Items = items;
            PortfolioName = portfolioName;
        }

        public string PortfolioName { get; }
        public List<InvoiceLineItem> Items { get; }
        
        private DateTime _RequestedOn { get; }
        private Guid _UniqueGuid { get; }

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
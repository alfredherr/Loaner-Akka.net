using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    public class PortfolioStateViewModel
    {
        public string PortfolioName { get; set; }

        public decimal TotalBalance { get; set; }

        public int AccountCount { get; set; }

        public DateTime AsOfDate { get; set; }
    }
}
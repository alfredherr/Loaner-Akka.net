namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    using System;

    public class PortfolioState
    {
        
       
        public string PortfolioName { get; set; }
 
        public decimal TotalBalance { get; set; }
        
        public int AccountCount { get; set; }
        
        public DateTime AsOfDate { get; set; }
        
       
    }
}

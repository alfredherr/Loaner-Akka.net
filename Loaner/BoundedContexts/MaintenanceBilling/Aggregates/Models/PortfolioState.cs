namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    using System;

    public class PortfolioState
    {
        
        public long ID { get; set; }

       
        public string Name { get; set; }
 
        public decimal TotalBalance { get; set; }

        
        public int AccountCount { get; set; }

        
        public DateTime AsOfDate { get; set; }
        
       
    }
}

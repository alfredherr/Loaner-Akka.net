namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models

{
    using DomainModels;
    using System;
    
    public class AccountStateKafka
    {
        public long ID { get; set; }

        public long PortfolioID { get; set; }

        public long UserID { get; set; }

        public decimal CurrentBalance { get; set; }

        public AccountStatus AccountStatus { get; set; }
        
        public DateTime AsOfDate { get; set; }

        public DateTime LastPaymentDate { get; set; }

        public decimal LastPaymentAmount { get; set; }

        public int DaysDelinquent { get; set; }

    }
    
}
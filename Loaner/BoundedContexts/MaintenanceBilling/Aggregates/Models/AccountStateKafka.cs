using System.Transactions;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models

{
    using DomainModels;
    using System;

    public class AccountStateKafka
    {
        public AccountStateKafka(string accountNumber, string userName, string portfolioNumber, decimal currentBalance, AccountStatus accountStatus, DateTime asOfDate, DateTime lastPaymentDate, decimal lastPaymentAmount, int daysDelinquent, string accountInventory)
        {
            AccountNumber = accountNumber;
            UserName = userName;
            PortfolioNumber = portfolioNumber;
            CurrentBalance = currentBalance;
            AccountStatus = accountStatus;
            AsOfDate = asOfDate;
            LastPaymentDate = lastPaymentDate;
            LastPaymentAmount = lastPaymentAmount;
            DaysDelinquent = daysDelinquent;
            AccountInventory = accountInventory;
        }
        public string AccountNumber { get; set; }
        public string UserName { get; set; }
        public string PortfolioNumber { get; set; }
        public decimal CurrentBalance { get; set; }
        public AccountStatus AccountStatus { get; set; }
        public DateTime AsOfDate { get; set; }
        public DateTime LastPaymentDate { get; set; }
        public decimal LastPaymentAmount { get; set; }
        public int DaysDelinquent { get; set; }
        public string AccountInventory { get; set; }

    }

}
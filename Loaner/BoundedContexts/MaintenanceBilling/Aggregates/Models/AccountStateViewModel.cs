using System;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models

{
    public class AccountStateViewModel
    {
        public AccountStateViewModel(string accountNumber, string userName, string portfolioName,
            decimal currentBalance, AccountStatus accountStatus, DateTime asOfDate, DateTime lastPaymentDate,
            decimal lastPaymentAmount, int daysDelinquent, string accountInventory)
        {
            AccountNumber = accountNumber;
            UserName = userName;
            PortfolioName = portfolioName;
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
        public string PortfolioName { get; set; }
        public decimal CurrentBalance { get; set; }
        public AccountStatus AccountStatus { get; set; }
        public DateTime AsOfDate { get; set; }
        public DateTime LastPaymentDate { get; set; }
        public decimal LastPaymentAmount { get; set; }
        public int DaysDelinquent { get; set; }
        public string AccountInventory { get; set; }
    }
}
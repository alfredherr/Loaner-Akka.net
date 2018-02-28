using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    public class AccountBoardingModel
    {
        public AccountBoardingModel(string portfolioName, AccountNumber accountNumber, double openingBalance,
            string inventory, string userName, DateTime lastPaymentDate, double lastPaymentAmount)
        {
            PortfolioName = portfolioName;
            AccountNumber = accountNumber;
            OpeningBalance = openingBalance;
            Inventory = inventory;
            UserName = userName;
            LastPaymentDate = lastPaymentDate;
            LastPaymentAmount = lastPaymentAmount;
        }

        public double LastPaymentAmount { get; }
        public DateTime LastPaymentDate { get; }
        public string PortfolioName { get; }
        public AccountNumber AccountNumber { get; }
        public double OpeningBalance { get; }
        public string Inventory { get; }
        public string UserName { get; }

        public override string ToString()
        {
            return
                $"{nameof(LastPaymentAmount)}: {LastPaymentAmount}, {nameof(LastPaymentDate)}: {LastPaymentDate}, {nameof(PortfolioName)}: {PortfolioName}, {nameof(AccountNumber)}: {AccountNumber.Instance}, {nameof(OpeningBalance)}: {OpeningBalance}, {nameof(Inventory)}: {Inventory}, {nameof(UserName)}: {UserName}";
        }
    }
}
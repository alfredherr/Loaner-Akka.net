using System.Collections.Generic;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class Account
    {
        public Account()
        {
        }

        public Account(string accountNumber)
        {
            AccountNumber = accountNumber;
        }

        public AccountStatus AccountStatus { get; set; }
        public string AccountNumber { get; set; }
        public double CurrentBalance { get; set; }
        public List<MaintenanceFee> Obligations { get; set; }

        public override string ToString()
        {
            return $"{AccountNumber}";
        }
    }
}
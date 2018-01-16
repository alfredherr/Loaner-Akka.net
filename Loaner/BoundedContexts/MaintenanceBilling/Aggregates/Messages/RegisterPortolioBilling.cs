using System;
using System.Collections.Generic;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class RegisterPortolioBilling
    {
        public RegisterPortolioBilling(string portfolioName, Dictionary<string, Tuple<double, double>> billedAccounts)
        {
            PortfolioName = portfolioName;
            AccountsBilled = billedAccounts;
        }

        public string PortfolioName { get; }

        public Dictionary<string, Tuple<double, double>> AccountsBilled { get; }
    }
}
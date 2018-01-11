using System.Collections.Generic;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class TellMeYourPortfolioStatus
    {
        public TellMeYourPortfolioStatus()
        {
            Accounts = new Dictionary<string, string>();
            Message = "";
        }

        public TellMeYourPortfolioStatus(string message)
        {
            Message = message;
            Accounts = new Dictionary<string, string>();
        }

        public TellMeYourPortfolioStatus(string message, Dictionary<string, string> accounts)
        {
            Message = message;
            Accounts = accounts;
        }

        public string Message { get; }
        public Dictionary<string, string> Accounts { get; }
    }
}
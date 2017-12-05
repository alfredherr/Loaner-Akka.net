using System.Collections.Generic;

namespace Demo.BoundedContexts.MaintenanceBilling.Events
{
    public class MyPortfolioStatus
    {
        public MyPortfolioStatus()
        {
            Accounts = new Dictionary<string, string>();
            Message = "";
        }

        public MyPortfolioStatus(string message)
        {
            Message = message;
            Accounts = new Dictionary<string, string>();
        }

        public MyPortfolioStatus(string message, Dictionary<string, string> accounts)
        {
            Message = message;
            Accounts = accounts;
        }

        public string Message { get; }
        public Dictionary<string, string> Accounts { get; }
    }
}
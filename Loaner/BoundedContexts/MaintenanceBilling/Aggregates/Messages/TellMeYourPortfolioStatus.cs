using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class TellMeYourPortfolioStatus
    {
        public TellMeYourPortfolioStatus()
        {
            Message = "";
            PortfolioStateViewModel = new PortfolioStateViewModel();
        }

        public TellMeYourPortfolioStatus(string message)
        {
            Message = message;
            PortfolioStateViewModel = new PortfolioStateViewModel();
        }

        public TellMeYourPortfolioStatus(string message, PortfolioStateViewModel portfolioStateViewModel)
        {
            Message = message;
            PortfolioStateViewModel = portfolioStateViewModel;
        }

        public string Message { get; }
        public PortfolioStateViewModel PortfolioStateViewModel { get; }
    }

    public class FailedListOfAccounts
    {
        public FailedListOfAccounts()
        {
            ListOfAccounts = new List<BusinessRuleApplicationResultModel>();
        }

        public FailedListOfAccounts(List<BusinessRuleApplicationResultModel> accounts)
        {
            ListOfAccounts = accounts;
        }
        public  List<BusinessRuleApplicationResultModel> ListOfAccounts { get; private set; }
    }
}
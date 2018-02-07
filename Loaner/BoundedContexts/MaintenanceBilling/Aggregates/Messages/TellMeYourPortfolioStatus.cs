using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;

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
}
using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class TellMeYourPortfolioStatus
    {
        public TellMeYourPortfolioStatus()
        {

            Message = "";
            PortfolioState = new PortfolioState();

        }

        public TellMeYourPortfolioStatus(string message)
        {
            Message = message;
            PortfolioState = new PortfolioState();
        }

        public TellMeYourPortfolioStatus(string message, PortfolioState portfolioState)
        {
            Message = message;
            PortfolioState = portfolioState;

        }

        public string Message { get; }
        public PortfolioState PortfolioState { get; }
    }
}
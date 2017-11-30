using System.Collections.Generic;

namespace Loaner.api.Models
{
    public class SupervisedPortfolios 
    {
        public SupervisedPortfolios()
        {
            Portfolios = new Dictionary<string, string>();
        }
        public SupervisedPortfolios(string message, Dictionary<string,string> portfolios)
        {
            Message = message;
            Portfolios = portfolios;
        }

        public string Message
        {
            get;
        }

        public Dictionary<string, string> Portfolios
        {
            get; 
        }
    }
}
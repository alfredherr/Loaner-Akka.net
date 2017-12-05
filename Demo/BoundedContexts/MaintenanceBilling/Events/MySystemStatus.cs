using System.Collections.Generic;

namespace Demo.BoundedContexts.MaintenanceBilling.Events
{
    public class MySystemStatus
    {
        public MySystemStatus()
        {
            Portfolios = new Dictionary<string, string>();
            Message = "";
        }

        public MySystemStatus(string message)
        {
            Message = message;
            Portfolios = new Dictionary<string, string>();
        }

        public MySystemStatus(string message, Dictionary<string, string> portfolios)
        {
            Message = message;
            Portfolios = portfolios;
        }

        public string Message { get; }
        public Dictionary<string, string> Portfolios { get; }
    }
}
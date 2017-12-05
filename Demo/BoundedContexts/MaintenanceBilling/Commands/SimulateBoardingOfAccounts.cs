namespace Demo.BoundedContexts.MaintenanceBilling.Commands
{
    public class SimulateBoardingOfAccounts  
    {
        public SimulateBoardingOfAccounts(string clientName, string clientAccountsFilePath, string obligationsFilePath)
        {
            ClientName = clientName;
            ClientAccountsFilePath = clientAccountsFilePath;
            ObligationsFilePath = obligationsFilePath;
        }

        public string ClientName { get; }
        public string ClientAccountsFilePath { get; }
        public string ObligationsFilePath { get; } 
    }
}
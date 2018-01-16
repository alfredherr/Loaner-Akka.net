namespace Loaner.API.Models
{
    public class SimulateBoardingOfAccountModel
    {
        public SimulateBoardingOfAccountModel()
        {
        }

        public SimulateBoardingOfAccountModel(string clientName, string clientAccountsFilePath,
            string obligationsFilePath)
        {
            ClientAccountsFilePath = clientAccountsFilePath;
            ClientName = clientName;
            ObligationsFilePath = obligationsFilePath;
        }

        public string ClientName { get; set; }
        public string ClientAccountsFilePath { get; set; }
        public string ObligationsFilePath { get; set; }
    }
}
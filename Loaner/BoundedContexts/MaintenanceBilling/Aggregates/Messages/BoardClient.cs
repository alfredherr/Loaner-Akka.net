using Akka.Actor;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    public class BoardClient
    {
        public BoardClient(
            SimulateBoardingOfAccounts client
            , string accountsFilePath
            , string obligationsFilePath
            , IActorRef boardingRouter)
        {
            Client = client;
            AccountsFilePath = accountsFilePath;
            ObligationsFilePath = obligationsFilePath;
            BoardingRouter = boardingRouter;
        }

        public IActorRef BoardingRouter { get; private set; }
        public SimulateBoardingOfAccounts Client { get; private set; }
        public string AccountsFilePath { get; private set; }
        public string ObligationsFilePath { get; private set; }
    }
}
using Akka.Actor;

namespace Loaner.BoundedContexts.MaintenanceBilling.Commands
{
    public class AskToBeSupervised
    {
        public AskToBeSupervised(string portfolio,IActorRef whoAmIAsking)
        {
            MyNewParent = whoAmIAsking;
            Portfolio = portfolio;
        }

        public IActorRef MyNewParent { get; }
        
        public string Portfolio { get; }
    }
}
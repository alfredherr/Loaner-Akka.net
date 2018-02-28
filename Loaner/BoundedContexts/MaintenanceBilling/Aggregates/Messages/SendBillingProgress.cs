using Akka.Actor;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class SendBillingProgress
    {
        public SendBillingProgress(IActorRef toWhom)
        {
            ToWhom = toWhom;
        }

        public IActorRef ToWhom { get; }
    }
}
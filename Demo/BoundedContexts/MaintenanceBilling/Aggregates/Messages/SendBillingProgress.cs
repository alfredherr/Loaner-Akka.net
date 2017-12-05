using System.Diagnostics;
using Akka.Actor;

namespace Demo.BoundedContexts.MaintenanceBilling.Aggregates.Messages
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
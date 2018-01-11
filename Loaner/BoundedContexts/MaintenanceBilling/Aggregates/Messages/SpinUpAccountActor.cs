using System.Collections.Generic;
using Akka.Actor;
using Loaner.BoundedContexts.MaintenanceBilling.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    internal class SpinUpAccountActor
    {
        public SpinUpAccountActor(
            string portfolio ,
            string accountNumber,
            List<MaintenanceFee> oligations,
            IActorRef supervisor)
        {
            Portfolio = portfolio;
            AccountNumber = accountNumber;
            Obligations = oligations;
            Supervisor = supervisor;
        }
        public string Portfolio { get; }
        public string AccountNumber { get; }
        public List<MaintenanceFee> Obligations { get; }
        public IActorRef Supervisor { get; }
    }
}
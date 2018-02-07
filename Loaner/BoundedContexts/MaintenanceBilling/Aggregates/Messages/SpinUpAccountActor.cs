using System.Collections.Generic;
using Akka.Actor;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    internal class SpinUpAccountActor
    {
        public SpinUpAccountActor(
            string portfolio,
            string accountNumber,
            List<MaintenanceFee> oligations,
            IActorRef supervisor, AccountBoardingModel boardingModel)
        {
            Portfolio = portfolio;
            AccountNumber = accountNumber;
            Obligations = oligations;
            Supervisor = supervisor;
            BoardingModel = boardingModel;
        }

        public AccountBoardingModel BoardingModel { get; }
        public string Portfolio { get; }
        public string AccountNumber { get; }
        public List<MaintenanceFee> Obligations { get; }
        public IActorRef Supervisor { get; }
    }
}
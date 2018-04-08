using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using NLog.Web.LayoutRenderers;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    public class FetchAccountBusinessRules : ICloneable
    {
        public FetchAccountBusinessRules(double totalBilledAmount, string client, string portfolioName,
            AccountState accountState, BillingAssessment command, IActorRef accountRef)
        {
            TotalBilledAmount = totalBilledAmount;
            Client = client;
            PortfolioName = portfolioName;
            AccountState = accountState;
            Command = command;
            AccountRef = accountRef;
        }

        public double TotalBilledAmount { get; private set; } //TODO tis has to be moved somewhere else
        public string Client { get; private set; }
        public string PortfolioName { get; private set; }
        public AccountState AccountState { get; private set; }
        public BillingAssessment Command { get; set; }
       
        public IActorRef AccountRef { get; private set; }

        private FetchAccountBusinessRules(FetchAccountBusinessRules abr)
        {
            TotalBilledAmount = abr.TotalBilledAmount;
            Client = abr.Client;
            PortfolioName = abr.PortfolioName;
            AccountState = abr.AccountState;
            Command = abr.Command;
            AccountRef = abr.AccountRef;
        }

        public override string ToString()
        {
            return 
                $"TotalBilledAmount {TotalBilledAmount } \n" +
                $"Client {Client } \n" +
                $"PortfolioName {PortfolioName } \n" +
                $"AccountState {AccountState} \n" +
                $"Command {Command} \n" +
                $"AccountRef {AccountRef.Path.Name}";
        }

        public object Clone()
        {
            return new FetchAccountBusinessRules(this);
        }
    }
}
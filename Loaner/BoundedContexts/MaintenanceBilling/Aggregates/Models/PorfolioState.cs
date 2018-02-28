using System;
using System.Collections.Generic;
using Akka.Actor;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    public class PorfolioState
    {
        public DateTime LastBootedOn;
        public int ScheduledCallsToInfo = 0;

        public PorfolioState()
        {
            SupervizedAccounts = new List<AccountUnderSupervision>();
        }

        public List<AccountUnderSupervision> SupervizedAccounts { get; }
    }

    public class AccountUnderSupervision
    {
        public AccountUnderSupervision(string accountNumber)
        {
            AccountNumber = accountNumber;
        }

        public string AccountNumber { get; }

        public double LastTransactionAmount { get; set; }

        public double BalanceAfterLastTransaction { get; set; }


        public IActorRef AccountActorRef { get; set; }
    }
}
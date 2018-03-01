using System;
using System.Collections.Generic;
using Akka.Actor;
using System.Linq;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models {
    public class PorfolioState : ICloneable {
        public DateTime LastBootedOn;
        public int ScheduledCallsToInfo = 0;

        public List<AccountUnderSupervision> SupervizedAccounts { get; }

        public PorfolioState () {
            SupervizedAccounts = new List<AccountUnderSupervision> ();
        }
        public PorfolioState (List<AccountUnderSupervision> accounts, DateTime lastBootedOn, int scheduledCallsToInfo) {
            SupervizedAccounts = accounts;
            LastBootedOn = lastBootedOn;
            ScheduledCallsToInfo = scheduledCallsToInfo;

        }

        public object Clone () {
            List<AccountUnderSupervision> accounts = this.SupervizedAccounts?.ToList() ?? new List<AccountUnderSupervision> ();
            return new PorfolioState (accounts, this.LastBootedOn, this.ScheduledCallsToInfo);
        }
    }

    public class AccountUnderSupervision {
        public AccountUnderSupervision (string accountNumber) {
            AccountNumber = accountNumber;
        }

        public string AccountNumber { get; }

        public double LastTransactionAmount { get; set; }

        public double BalanceAfterLastTransaction { get; set; }

        public IActorRef AccountActorRef { get; set; }
    }
}
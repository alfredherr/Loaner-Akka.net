using System;
using Akka.Actor;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    public class AccountUnderSupervision : ICloneable {
       
        public AccountUnderSupervision (string accountNumber) {
            AccountNumber = accountNumber;
        }

        private AccountUnderSupervision (string accountNumber, double lastBilledAmount, double balanceAfterLastTransaction) {
            AccountNumber = accountNumber;
            LastBilledAmount = lastBilledAmount;
            BalanceAfterLastTransaction = balanceAfterLastTransaction;
        }
        public string AccountNumber { get; }

        public double LastBilledAmount { get; set; }

        public double BalanceAfterLastTransaction { get; set; }

        public IActorRef AccountActorRef { get; set; }
        
        public object Clone()
        {
           return new AccountUnderSupervision(this.AccountNumber,this.LastBilledAmount,this.BalanceAfterLastTransaction);
        }
    }
}
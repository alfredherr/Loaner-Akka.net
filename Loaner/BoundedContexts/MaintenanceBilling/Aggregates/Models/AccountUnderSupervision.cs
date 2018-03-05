using System;
using Akka.Actor;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    public class AccountUnderSupervision : ICloneable {
       
        public AccountUnderSupervision (string accountNumber, double balanceAfterLastTransaction) {
            AccountNumber = accountNumber;
            BalanceAfterLastTransaction = balanceAfterLastTransaction;
        }

        private AccountUnderSupervision (string accountNumber, double lastBilledAmount, double balanceAfterLastTransaction, IActorRef accountActorRef) {
            AccountNumber = accountNumber;
            LastBilledAmount = lastBilledAmount;
            BalanceAfterLastTransaction = balanceAfterLastTransaction;
            AccountActorRef = accountActorRef;
            //Console.WriteLine($"[AccountUnderSupervision]: BalanceAfterLastTransaction = {BalanceAfterLastTransaction }");
        }
        public string AccountNumber { get; }

        public double LastBilledAmount { get; set; }

        public double BalanceAfterLastTransaction { get; set; }

        public IActorRef AccountActorRef { get; set; }
        
        public object Clone()
        {
           return new AccountUnderSupervision(this.AccountNumber,this.LastBilledAmount,this.BalanceAfterLastTransaction,AccountActorRef);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    public class PorfolioState : ICloneable
    {
        public DateTime LastBootedOn { get; set; }

        public int ScheduledCallsToInfo { get; set; }

        public decimal CurrentPortfolioBalance { get; private set; }
        
        

        public Dictionary<string, AccountUnderSupervision> SupervizedAccounts { get; }

        public PorfolioState()
        {
            SupervizedAccounts = new Dictionary<string, AccountUnderSupervision>();
        }

        public decimal UpdateBalance()
        {
            CurrentPortfolioBalance = (decimal) SupervizedAccounts.Aggregate(0.0,
                (accumulator, next) => accumulator + next.Value.BalanceAfterLastTransaction);
            return CurrentPortfolioBalance;
        }

        private PorfolioState(Dictionary<string, AccountUnderSupervision> accounts, DateTime lastBootedOn,
            int scheduledCallsToInfo, decimal currentPortfolioBalance)
        {
            SupervizedAccounts = accounts;
            LastBootedOn = lastBootedOn;
            ScheduledCallsToInfo = scheduledCallsToInfo;
            CurrentPortfolioBalance = currentPortfolioBalance;
        }

        public object Clone()
        {
            UpdateBalance();
            var dict = this.SupervizedAccounts.ToDictionary(
                x => x.Key,
                x => (AccountUnderSupervision) x.Value.Clone());

            //Console.WriteLine($"[PorfolioState]: Current balance, right before cloning: {CurrentPortfolioBalance :C}");
            return new PorfolioState(
                dict
                , this.LastBootedOn
                , this.ScheduledCallsToInfo
                , this.CurrentPortfolioBalance
            );

        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    public class PorfolioState : ICloneable
    {
        public DateTime LastBootedOn;
        
        public int ScheduledCallsToInfo = 0;

        public Dictionary<string, AccountUnderSupervision> SupervizedAccounts { get;  }

        public PorfolioState()
        {
            SupervizedAccounts = new Dictionary<string,AccountUnderSupervision>();
        }

        private PorfolioState(Dictionary<string,AccountUnderSupervision> accounts, DateTime lastBootedOn, int scheduledCallsToInfo)
        {
            SupervizedAccounts = accounts;
            LastBootedOn = lastBootedOn;
            ScheduledCallsToInfo = scheduledCallsToInfo;
        }

        public object Clone()
        {
            
         var dict = this.SupervizedAccounts.ToDictionary(
                x => x.Key,
                x => (AccountUnderSupervision) x.Value.Clone() );
            
            return new PorfolioState(
                dict
                , this.LastBootedOn
                , this.ScheduledCallsToInfo);
        }
    }
}
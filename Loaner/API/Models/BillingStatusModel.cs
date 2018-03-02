using System;
using System.Collections.Generic;
using Akka.Util.Internal;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;

namespace Loaner.API.Models
{
    public class BillingStatusModel
    {
        public BillingStatusModel()
        {
            SummarizedBillingStatus = new Dictionary<string, int>();
        }

        public BillingStatusModel(PortfolioBillingStatus status) : this()
        {
            BillingStatus = status.Billings;
        }

        private Dictionary<string, Dictionary<string, Tuple<double, double>>> BillingStatus { get; }

        public string AccountsBilled {
            get => _AccountsBilled.ToString("##,#");
            set => AccountsBilled = value;
        }
        
        public string  AmountBilled {
            get => _AmountBilled.ToString("C");
            set => AmountBilled = value;
        }
        
        public string BalanceAfterBilling  {
            get => _BalanceAfterBilling.ToString("C");
            set => BalanceAfterBilling = value;
        }

        private int _AccountsBilled { get; set; }
 
        private double _AmountBilled { get; set; }
 
        private double _BalanceAfterBilling { get; set; }

        public Dictionary<string, int> SummarizedBillingStatus { get; set; }

        public void Summarize()
        {
            foreach (var billingStatusBilling in BillingStatus)
            {
                SummarizedBillingStatus.AddOrSet(billingStatusBilling.Key, billingStatusBilling.Value.Keys.Count);

                foreach (var account in billingStatusBilling.Value)
                {
                    _AccountsBilled++;
                    _AmountBilled += account.Value.Item1;
                    _BalanceAfterBilling += account.Value.Item2;
                }
            }
        }
    }
}
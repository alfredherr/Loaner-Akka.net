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

        public int AccountsBilled { get; set; }
        public double AmountBilled { get; set; }
        public double BalanceAfterBilling { get; set; }

        public Dictionary<string, int> SummarizedBillingStatus { get; set; }

        public void Summarize()
        {
            foreach (var billingStatusBilling in BillingStatus)
            {
                SummarizedBillingStatus.AddOrSet(billingStatusBilling.Key, billingStatusBilling.Value.Keys.Count);

                foreach (var account in billingStatusBilling.Value)
                {
                    AccountsBilled++;
                    AmountBilled += account.Value.Item1;
                    BalanceAfterBilling += account.Value.Item2;
                }
            }
        }
    }
}
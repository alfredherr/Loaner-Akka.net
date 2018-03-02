using System;
 using System.Collections.Generic;
 
 namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
 {
     public class PortfolioBillingStatus
     {
         public PortfolioBillingStatus(Dictionary<string, Dictionary<string, Tuple<double, double>>>
             billings)
         {
             Billings = billings ?? new Dictionary<string, Dictionary<string, Tuple<double, double>>>();
         }
 
         public Dictionary<string, Dictionary<string, Tuple<double, double>>> Billings { get; }
     }
 }
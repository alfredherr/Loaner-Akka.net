using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class MaintenanceFee : IObligation , ICloneable
    {
        public MaintenanceFee()
        {
        }

        [JsonConstructor]
        public MaintenanceFee(string obligationNumber, double openingBalance,ObligationStatus status)
        {
            ObligationNumber = obligationNumber;
            CurrentBalance = openingBalance;
            Status = ObligationStatus.Active;
            Transactions = new List<FinancialTransaction>();
            Status = status;
        }

        private MaintenanceFee(string obligationNumber,ObligationStatus status, double currentBalance, List<FinancialTransaction> transactions)
        {
            ObligationNumber = obligationNumber;
            Status = status;
            CurrentBalance = currentBalance;
            Transactions = transactions.Select( x=> x.Clone() ).Cast<FinancialTransaction>().ToList();
        }
        public object Clone()
        {
            return new MaintenanceFee(ObligationNumber,Status,CurrentBalance,GetTransactions());
        }
        
        [JsonProperty(Order = 1)]
        public string ObligationNumber { get; }

        [JsonProperty(Order = 2)]
        public ObligationStatus Status { get; private set; }


        [JsonProperty(Order = 3)]
        public double CurrentBalance { get; private set; }


        [JsonProperty(Order = 4)]
        public List<FinancialTransaction> Transactions { get; private set; }

        public double PostTransaction(FinancialTransaction occurred)
        {
            Transactions.Add(occurred);
            return UpdateCurrentBalance();
        }

        private double UpdateCurrentBalance()
        {
            CurrentBalance = Transactions.Sum(x => x.TransactionAmount);
            return CurrentBalance;
        }

        public List<FinancialTransaction> GetTransactions()
        {
            return Transactions.Select(x => x.Clone()).Cast<FinancialTransaction>().ToList();
        }

        public MaintenanceFee SetStatus(ObligationStatus status)
        {
            Status = status;
            return this;
        }

    }
}
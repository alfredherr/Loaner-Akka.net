
namespace Loaner.BoundedContexts.MaintenanceBilling.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    public interface IObligation
    {
    }

    public class MaintenanceFee : IObligation
    {
        public ObligationStatus Status { get; private set; }
        public List<FinancialTransaction> Transactions { get; private set; }
        
        public string ObligationNumber { get;  }
        public double CurrentBalance { get; private set; }

        public MaintenanceFee()
        {
            
        }
        
        [JsonConstructor]
        public MaintenanceFee(string obligationNumber, double openingBalance)
        {
            ObligationNumber = obligationNumber;
            CurrentBalance = openingBalance;
            Status = ObligationStatus.Active;
            Transactions = new List<FinancialTransaction>();
            
        }

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
            return Transactions;
        }

        public MaintenanceFee SetStatus(ObligationStatus status)
        {
            Status = status;
            return this;
        }

         
    }
    
    
    public class Bucket 
    {
       
        public Bucket(string bucket, double balance)
        {
            Name = bucket;
            Amount = balance;
        }

        
        public string Name { get; }
        public double Amount { get; set; }
    }
}
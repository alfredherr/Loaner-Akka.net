using Akka.Streams;

namespace Loaner.BoundedContexts.MaintenanceBilling.Models
{
    public interface IFinancialBucket
    {
        string Name { get; }
        double Amount { get;  set; }        
    }

   
    public class Tax : IFinancialBucket
    {
        public string Name => "Tax";
        public double Amount { get; set; }
    }
    public class Interest : IFinancialBucket
    {
        public string Name => "Interest";
        public double Amount { get; set; }
    }    
    public class Reserve : IFinancialBucket
    {
        public string Name => "Reserve";
        public double Amount { get; set; }
    }    
    public class LoanPrincipal : IFinancialBucket
    {
        public string Name => "LoanPrincipal";
        public double Amount { get; set; }
    }
    public class Dues : IFinancialBucket
    {
        public string Name => "Dues";
        public double Amount { get; set; }
      
    }
  
}
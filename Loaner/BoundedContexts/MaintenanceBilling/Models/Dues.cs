namespace Loaner.BoundedContexts.MaintenanceBilling.Models
{
    public class Dues : IFinancialBucket
    {
        public string Name => "Dues";
        public double Amount { get; set; }
      
    }
}
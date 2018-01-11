namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class Dues : IFinancialBucket
    {
        public string Name => "Dues";
        public double Amount { get; set; }
      
    }
}
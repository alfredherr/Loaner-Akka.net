namespace Loaner.BoundedContexts.MaintenanceBilling.Models
{
    public class Reserve : IFinancialBucket
    {
        public string Name => "Reserve";
        public double Amount { get; set; }
    }
}
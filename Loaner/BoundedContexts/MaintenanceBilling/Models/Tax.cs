namespace Loaner.BoundedContexts.MaintenanceBilling.Models
{
    public class Tax : IFinancialBucket
    {
        public string Name => "Tax";
        public double Amount { get; set; }
    }
}
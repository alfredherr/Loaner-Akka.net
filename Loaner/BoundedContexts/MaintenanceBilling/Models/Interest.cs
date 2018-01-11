namespace Loaner.BoundedContexts.MaintenanceBilling.Models
{
    public class Interest : IFinancialBucket
    {
        public string Name => "Interest";
        public double Amount { get; set; }
    }
}
namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class Tax : IFinancialBucket
    {
        public string Name => "Tax";
        public double Amount { get; set; }
    }
}
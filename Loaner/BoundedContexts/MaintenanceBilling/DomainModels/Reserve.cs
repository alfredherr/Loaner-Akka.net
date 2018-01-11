namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class Reserve : IFinancialBucket
    {
        public string Name => "Reserve";
        public double Amount { get; set; }
    }
}
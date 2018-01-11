namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class Interest : IFinancialBucket
    {
        public string Name => "Interest";
        public double Amount { get; set; }
    }
}
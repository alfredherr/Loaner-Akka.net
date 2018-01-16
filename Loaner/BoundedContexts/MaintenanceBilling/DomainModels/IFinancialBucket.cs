namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public interface IFinancialBucket
    {
        string Name { get; }
        double Amount { get; set; }
    }
}
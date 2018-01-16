namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class Reserve : IFinancialBucket
    {
        public Reserve()
        {
        }

        public Reserve(double amount)
        {
            Amount = amount;
        }

        public string Name => "Reserve";
        public double Amount { get; set; }
    }
}
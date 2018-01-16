namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class Interest : IFinancialBucket
    {
        public Interest()
        {
        }

        public Interest(double amount)
        {
            Amount = amount;
        }

        public string Name => "Interest";
        public double Amount { get; set; }
    }
}
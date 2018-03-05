namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class Adjustment : IFinancialBucket
    {
        public Adjustment()
        {
        }

        public Adjustment(double amount)
        {
            Amount = amount;
        }

        public string Name => "Adjustment";
        public double Amount { get; set; }
    }
}
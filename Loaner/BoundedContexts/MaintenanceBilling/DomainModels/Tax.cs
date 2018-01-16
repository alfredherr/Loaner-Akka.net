namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class Tax : IFinancialBucket
    {
        public Tax()
        {
        }

        public Tax(double amount)
        {
            Amount = amount;
        }

        public string Name => "Tax";
        public double Amount { get; set; }
    }
}
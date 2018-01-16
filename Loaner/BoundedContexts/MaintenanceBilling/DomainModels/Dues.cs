namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class Dues : IFinancialBucket
    {
        public Dues()
        {
        }

        public Dues(double amount)
        {
            Amount = amount;
        }

        public string Name => "Dues";
        public double Amount { get; set; }
    }
}
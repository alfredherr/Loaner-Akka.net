namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class CreditCardPayment : IFinancialBucket
    {
        public CreditCardPayment()
        {
        }

        public CreditCardPayment(double amount)
        {
            Amount = amount;
        }

        public string Name => "CreditCardPayment";
        public double Amount { get; set; }
    }
}
namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class LoanPrincipal : IFinancialBucket
    {
        public LoanPrincipal()
        {
        }

        public LoanPrincipal(double amount)
        {
            Amount = amount;
        }

        public string Name => "LoanPrincipal";
        public double Amount { get; set; }
    }
}
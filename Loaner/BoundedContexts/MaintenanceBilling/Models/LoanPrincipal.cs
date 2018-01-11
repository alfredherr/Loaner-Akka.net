namespace Loaner.BoundedContexts.MaintenanceBilling.Models
{
    public class LoanPrincipal : IFinancialBucket
    {
        public string Name => "LoanPrincipal";
        public double Amount { get; set; }
    }
}
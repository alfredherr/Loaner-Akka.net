namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class LoanPrincipal : IFinancialBucket
    {
        public string Name => "LoanPrincipal";
        public double Amount { get; set; }
    }
}
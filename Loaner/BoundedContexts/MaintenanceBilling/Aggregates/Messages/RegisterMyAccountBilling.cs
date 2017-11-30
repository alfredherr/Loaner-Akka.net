namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class RegisterMyAccountBilling
    {
        public RegisterMyAccountBilling(string accountNumber, double amountBilled, double accountBalanceAfter)
        {
            AccountNumber = accountNumber;
            AmountBilled = amountBilled;
            AccountBalanceAfterBilling = accountBalanceAfter;
        }

        public double AmountBilled { get; }

        public double AccountBalanceAfterBilling { get; }

        public string AccountNumber { get; }
    }
}
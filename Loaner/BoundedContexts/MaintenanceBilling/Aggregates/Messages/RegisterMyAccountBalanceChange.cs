namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class RegisterMyAccountBalanceChange
    {
        public RegisterMyAccountBalanceChange(string accountNumber, double amountTransacted, double accountBalanceAfter)
        {
            AccountNumber = accountNumber;
            AmountTransacted = amountTransacted;
            AccountBalanceAfterTransaction = accountBalanceAfter;
        }

        public double AmountTransacted { get; }

        public double AccountBalanceAfterTransaction { get; }

        public string AccountNumber { get; }
    }
}
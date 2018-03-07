namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates
{
    public class AccountUnderSupervisionBalanceChanged
    {
        public AccountUnderSupervisionBalanceChanged(string accountNumber, double newBalance)
        {
            AccountNumber = accountNumber;
            NewAccountBalance = newBalance;
        }

        public string AccountNumber { get; }
        public double NewAccountBalance { get; }
    }
}
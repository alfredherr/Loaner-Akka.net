namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    internal class AccountNumber
    {
        public AccountNumber(string accountNumber)
        {
            Instance = accountNumber;
        }

        public string Instance { get; }

        public override int GetHashCode()
        {
            return Instance.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            AccountNumber test = obj as AccountNumber;
            return test.Instance == this.Instance;
        }
    }
}
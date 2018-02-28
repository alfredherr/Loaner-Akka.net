namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    public class AccountNumber
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
            var test = obj as AccountNumber;
            return test.Instance == Instance;
        }
    }
}
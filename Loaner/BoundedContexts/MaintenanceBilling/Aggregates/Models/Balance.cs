namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    internal class Balance
    {
        public Balance(double amount)
        {
            Instance = amount;
        }

        public double Instance { get; }

        public override int GetHashCode()
        {
            return Instance.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var test = obj as Balance;
            return test.Instance == Instance;
        }
    }
}
namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    internal class PortfolioName
    {
        public PortfolioName(string name)
        {
            Instance = name;
        }

        public string Instance { get; }

        public override int GetHashCode()
        {
            return Instance.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var test = obj as PortfolioName;
            return test.Instance == Instance;
        }
    }
}
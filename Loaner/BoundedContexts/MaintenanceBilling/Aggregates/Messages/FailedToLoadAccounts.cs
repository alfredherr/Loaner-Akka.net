namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class FailedToLoadAccounts
    {
        public FailedToLoadAccounts(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
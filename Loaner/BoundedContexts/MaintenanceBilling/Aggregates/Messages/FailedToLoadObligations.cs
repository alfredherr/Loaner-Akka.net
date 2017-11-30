namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages
{
    public class FailedToLoadObligations
    {
        public FailedToLoadObligations(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}
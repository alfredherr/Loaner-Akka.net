namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class Note
    {
        public Note(string message)
        {
            Message = message;
        }

        public string Message { get; set; }
    }
}
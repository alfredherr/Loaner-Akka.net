using System;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    public class StateLog
    {
        public StateLog(string eventName, Guid id, DateTime date)
        {
            EventName = eventName;
            EventId = id;
            EventDate = date;
        }

        public Guid EventId { get; }
        public DateTime EventDate { get; }
        public string EventName { get; }
    }
}
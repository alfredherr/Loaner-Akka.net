using System;
using Newtonsoft.Json;

namespace Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models
{
    public class StateLog : ICloneable
    {
        public StateLog(string eventName, string eventMessage, Guid id, DateTime date)
        {
            EventName = eventName;
            EventId = id;
            EventDate = date;
            EventMessage = eventMessage;
        }

        [JsonProperty(Order = 1)]
        public Guid EventId { get; }

        [JsonProperty(Order = 2)]
        public DateTime EventDate { get; }

        [JsonProperty(Order = 3)]
        public string EventName { get; }

        [JsonProperty(Order = 4)]
        public string EventMessage { get; }

        public object Clone()
        {
            return new StateLog( EventName, EventMessage, EventId , EventDate );
        }
    }
}
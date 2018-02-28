using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AccountStatus
    {
        Active = 0,
        Inactive,
        Cancelled,
        Created,
        Boarded,
        Upgraded,
        Removed,
        Closed
    }
}
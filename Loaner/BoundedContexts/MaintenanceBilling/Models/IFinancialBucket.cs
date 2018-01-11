using Akka.Streams;

namespace Loaner.BoundedContexts.MaintenanceBilling.Models
{
    public interface IFinancialBucket
    {
        string Name { get; }
        double Amount { get;  set; }        
    }
}
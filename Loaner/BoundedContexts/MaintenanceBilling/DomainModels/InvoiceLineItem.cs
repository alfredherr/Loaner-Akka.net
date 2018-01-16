using Loaner.BoundedContexts.MaintenanceBilling.DomainModels.Serizalizers.Json;
using Newtonsoft.Json;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
{
    public class InvoiceLineItem
    {
        public InvoiceLineItem(IFinancialBucket item)
        {
            Item = item;
        }

       // [JsonConverter(typeof(FinancialBucketConverter))]
        public IFinancialBucket Item { get; set; }
    }
}
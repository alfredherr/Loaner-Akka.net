using Newtonsoft.Json;

namespace Loaner.BoundedContexts.MaintenanceBilling.Models
{
    public class InvoiceLineItem
    {
        public InvoiceLineItem()
        {
           
        }

        //[JsonConstructor]
        public InvoiceLineItem(IFinancialBucket item)
        {
            Item = item;
        }

        public IFinancialBucket Item { get; set; }
      
    }
}
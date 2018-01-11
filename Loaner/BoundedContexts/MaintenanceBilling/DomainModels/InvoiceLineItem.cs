namespace Loaner.BoundedContexts.MaintenanceBilling.DomainModels
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
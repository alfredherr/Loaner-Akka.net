using Newtonsoft.Json;

namespace Loaner.BoundedContexts.MaintenanceBilling.Models
{
    public class InvoiceLineItem
    {
        public InvoiceLineItem()
        {
           
        }

        //[JsonConstructor]
        public InvoiceLineItem(IFinancialBucket item, int units, double unitAmount, double totalAmount)
        {
            Item = item;
            Units = units;
            UnitAmount = unitAmount;
            TotalAmount = totalAmount;
        }

        public IFinancialBucket Item { get; set; }
        public int Units { get; set; }
        public double UnitAmount { get; set; }
        public double TotalAmount { get; set; }
    }
}
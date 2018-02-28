using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.API.Models
{
    public class SimulateAssessmentModel
    {
        //[JsonConverter(typeof(FinancialBucketConverter))]
        public List<InvoiceLineItem> LineItems { get; set; }
    }
}
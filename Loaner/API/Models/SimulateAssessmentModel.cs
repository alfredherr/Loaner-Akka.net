namespace Loaner.API.Models
{
    using System.Collections.Generic;
    using BoundedContexts.MaintenanceBilling.DomainModels;
    using Newtonsoft.Json;
    using BoundedContexts.MaintenanceBilling.DomainModels.Serizalizers.Json;


    public class SimulateAssessmentModel
    {
        //[JsonConverter(typeof(FinancialBucketConverter))]
        public List<InvoiceLineItem> LineItems { get; set; }
    }
}
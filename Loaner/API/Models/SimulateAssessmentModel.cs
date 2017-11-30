using System.Collections.Generic;
using System.Linq;
using Loaner.BoundedContexts.MaintenanceBilling.Models;

namespace Loaner.api.Models
{
    
    public class SimulateAssessmentModel
    {
      
        public List<InvoiceLineItem> LineItems { get; set; }
        
        
    }
}


using System;
using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.Commands
{
    public class BillingAssessment : IDomainCommand
    {
        public BillingAssessment()
        {
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
        }

        public BillingAssessment(List<InvoiceLineItem> lineItems) 
        {
           LineItems = lineItems ?? new List<InvoiceLineItem>();
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
        }

        public BillingAssessment(string accountNumber, List<InvoiceLineItem> lineItems) 
        {
            AccountNumber = accountNumber;
            LineItems = lineItems ?? new List<InvoiceLineItem>();
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
        }


        private DateTime _RequestedOn { get; }
        private Guid _UniqueGuid { get; }
        public string AccountNumber { get; }
        public List<InvoiceLineItem> LineItems { get; }

        public DateTime RequestedOn()
        {
            return _RequestedOn;
        }

        public Guid UniqueGuid()
        {
            return _UniqueGuid;
        }
    }
}
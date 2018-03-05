using System;
using System.Collections.Generic;
using Akka.Actor;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.DomainCommands
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

        public BillingAssessment(string accountNumber
            , List<InvoiceLineItem> lineItems
            , IActorRef businessRulesHandlingRouter
            , IActorRef businessRulesMapperRouter)
        {
            AccountNumber = accountNumber;
            LineItems = lineItems ?? new List<InvoiceLineItem>();
            _RequestedOn = DateTime.Now;
            _UniqueGuid = Guid.NewGuid();
            BusinessRulesHandlingRouter = businessRulesHandlingRouter;
            AccountBusinessMapperRouter = businessRulesMapperRouter;
        }

        public IActorRef BusinessRulesHandlingRouter { get; set; }
        public IActorRef AccountBusinessMapperRouter { get; set; }

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
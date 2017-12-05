using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates.StateModels;
using Demo.BoundedContexts.MaintenanceBilling.Events;
using Demo.BoundedContexts.MaintenanceBilling.Models;

namespace Demo.BoundedContexts.MaintenanceBilling.BusinessRules
{
    public class AnObligationMustBeActiveForBilling : IAccountBusinessRule
    {
        private readonly AccountState _accountState;
        private string _detailsGenerated;
        private List<IEvent> _eventsGenerated;
        private readonly List<InvoiceLineItem> _lineItems;

        public AnObligationMustBeActiveForBilling(AccountState accountState, List<InvoiceLineItem> lineItems)
        {
            _accountState = accountState;
            _lineItems = lineItems;
        }

        public bool Success { get; private set; }

        public string GetResultDetails()
        {
            return _detailsGenerated;
        }

        public List<IEvent> GetGeneratedEvents()
        {
            return _eventsGenerated;
        }

        public AccountState GetGeneratedState()
        {
            return _accountState;
        }

        /* Rule logic goes here. */
        public void RunRule()
        {
            Success = false;
            _eventsGenerated = new List<IEvent>();
            MaintenanceFee maintenanceFeeToUse = null;
            maintenanceFeeToUse = 
                _accountState
                .Obligations
                .Where( x => x.Value.Status == ObligationStatus.Active)
                .Select(y => y.Value).First();
            
            if (maintenanceFeeToUse != null)
            {
                foreach (var item in _lineItems)
                {
                    var @event =
                        new ObligationAssessedConcept(maintenanceFeeToUse.ObligationNumber, item.Item, item.TotalAmount);
                    _eventsGenerated.Add(@event);
                }

                _detailsGenerated = "THIS WORKED";
                Success = true;
            }
            else
            {
                _detailsGenerated = "No Active obligations on this account.";
            }
            
              
        }
    }
}
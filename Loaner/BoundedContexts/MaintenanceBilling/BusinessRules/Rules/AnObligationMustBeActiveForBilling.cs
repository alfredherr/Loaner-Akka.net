using System.Collections.Generic;
using System.Linq;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.StateModels;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.Commands;
using Loaner.BoundedContexts.MaintenanceBilling.Events;
using Loaner.BoundedContexts.MaintenanceBilling.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Rules
{
    public class AnObligationMustBeActiveForBilling : IAccountBusinessRule
    {
        /* Rule logic goes here. */
        public void RunRule(IDomainCommand command)
        {
            Success = false;
            _eventsGenerated = new List<IDomainEvent>();
            MaintenanceFee maintenanceFeeToUse = null;
            maintenanceFeeToUse =
                AccountState
                    .Obligations
                    .Where(x => x.Value.Status == ObligationStatus.Active)
                    .Select(y => y.Value).First();

            if (maintenanceFeeToUse != null)
            {
                foreach (var item in LineItems)
                {
                    var @event =
                        new ObligationAssessedConcept(maintenanceFeeToUse.ObligationNumber, item.Item);
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

        private AccountState AccountState { set; get; }
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated;
        private List<InvoiceLineItem> LineItems { get; set; }

        private (string Command, Dictionary<string, object> Parameters) CommandState { get; set; }

        public AnObligationMustBeActiveForBilling((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }


        public AnObligationMustBeActiveForBilling(AccountState accountState, List<InvoiceLineItem> lineItems)
        {
            AccountState = accountState;
            LineItems = lineItems;
        }

        public void SetAccountState(AccountState state)
        {
            AccountState = state;
        }

        public void SetCallingCommandState((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public void SetLineItems(List<InvoiceLineItem> lineItems)
        {
            LineItems = lineItems;
        }

        private bool Success { get; set; }

        public string GetResultDetails()
        {
            return _detailsGenerated;
        }

        public List<IDomainEvent> GetGeneratedEvents()
        {
            return _eventsGenerated;
        }

        public AccountState GetGeneratedState()
        {
            return AccountState;
        }

        public bool RuleAppliedSuccessfuly()
        {
            return Success;
        }
    }
}
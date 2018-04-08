using System;
using System.Collections.Generic;
using System.Linq;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Rules
{
    public class AnObligationMustBeActiveForBilling : IAccountBusinessRule
    {
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated;

        public AnObligationMustBeActiveForBilling((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }


        public AnObligationMustBeActiveForBilling(AccountState accountState, List<InvoiceLineItem> lineItems)
        {
            AccountState = accountState;
            LineItems = lineItems;
        }

        public AnObligationMustBeActiveForBilling()
        {
        }

        public AnObligationMustBeActiveForBilling(AccountState accountState)
        {
            AccountState = accountState;
        }

        private AccountState AccountState { set; get; }
        private List<InvoiceLineItem> LineItems { get; set; }

        private (string Command, Dictionary<string, object> Parameters) CommandState { get; set; }

        private bool Success { get; set; }

        /* Rule logic goes here. */
        public void RunRule(IDomainCommand command)
        {
            switch (command)
            {
                case BillingAssessment billing:
                    RunRule(billing);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void RunRule(BillingAssessment command)
        {
            LineItems = command.LineItems;
            try
            {
                Success = false;
                _eventsGenerated = new List<IDomainEvent>();
                var maintenanceFeeToUse =
                    AccountState
                        .Obligations
                        .Where(x => x.Value.Status == ObligationStatus.Active)
                        .Select(y => y.Value.ObligationNumber).FirstOrDefault();

                if (string.IsNullOrEmpty(maintenanceFeeToUse))
                {
                    _detailsGenerated = "No Active obligations on this account.";
                    return;
                }

                foreach (var item in LineItems)
                {
                    var @event =
                        new AccountBusinessRuleValidationSuccess(maintenanceFeeToUse,
                            "AccountBusinessRuleValidationSuccess on AnObligationMustBeActiveForBilling");
                    _eventsGenerated.Add(@event);
                }

                _detailsGenerated = "THIS WORKED";
                Success = true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"AnObligationMustBeActiveForBilling exception {e}");
                throw;
            }
        }


        public void SetAccountState(AccountState state)
        {
            AccountState = state ?? throw new ArgumentNullException("Why are you trying to set a null AccountState?");
        }

        public void SetCallingCommandState((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

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

        public void SetLineItems(List<InvoiceLineItem> lineItems)
        {
            LineItems = lineItems;
        }
    }
}
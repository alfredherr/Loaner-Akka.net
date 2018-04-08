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
    public class ClientSpecificRuleForCalculatingTax : IAccountBusinessRule
    {
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated;

        public ClientSpecificRuleForCalculatingTax((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public ClientSpecificRuleForCalculatingTax(AccountState accountState)
        {
            AccountState = accountState;
        }

        private AccountState AccountState { get; set; }

        private (string Command, Dictionary<string, object> Parameters) CommandState { get; set; }

        public bool Success { get; private set; }

        /* Rule logic goes here. */
        public void RunRule(IDomainCommand command)
        {    switch (command)
            {
                case BillingAssessment billing:
                    RunRule(billing);
                    break;
                default:
                    throw new NotImplementedException();
            }
           
        }
        public void RunRule(BillingAssessment com)
        {

            //Extract parameter Dues from Command
            var duesAmount = 0.00;
           
            foreach (var c in com.LineItems)
                if (c.Item.Name.Equals("Dues"))
                {
                    duesAmount = c.Item.Amount;
                    break;
                }

            var obligationToUse = AccountState.Obligations
                .FirstOrDefault(x => x.Value.Status == ObligationStatus.Active).Key;
            if (duesAmount <= 0.00 || string.IsNullOrEmpty(obligationToUse) )
            {
                _eventsGenerated = new List<IDomainEvent>
                {
                    new AccountBusinessRuleValidationFailure(
                        AccountState.AccountNumber,
                        "ClientSpecificRuleForCalculatingTax requires a 'Dues' amount be provided when billing."
                    )
                };
                ;
                Success = false;
                return;
            }

            _eventsGenerated = new List<IDomainEvent>
            {
                new TaxAppliedDuringBilling(
                    AccountState.AccountNumber,
                    obligationToUse,
                    decimal.Parse(((15.0 / 100) * duesAmount).ToString())
                )
            };
            _detailsGenerated = "THIS WORKED";
            Success = true;
        }

        public void SetAccountState(AccountState state)
        {
            AccountState = state;
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
    }
}
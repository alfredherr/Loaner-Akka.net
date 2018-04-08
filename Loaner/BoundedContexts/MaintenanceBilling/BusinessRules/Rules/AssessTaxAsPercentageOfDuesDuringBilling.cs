using System;
using System.Collections.Generic;
using System.Linq;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Exceptions;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Rules
{
    public class AssessTaxAsPercentageOfDuesDuringBilling : IAccountBusinessRule
    {
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated;

        public AssessTaxAsPercentageOfDuesDuringBilling(
            (string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public AssessTaxAsPercentageOfDuesDuringBilling(AccountState accountState)
        {
            AccountState = accountState;
        }

        private AccountState AccountState { get; set; }
        private (string Command, Dictionary<string, object> Parameters) CommandState { get; set; }

        private bool Success { get; set; }

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

            //Extract parameter TaxPercentageRate
            var taxRate = ExtractParameter("TaxPercentageRate");

            //Extract parameter Dues from Command
            var duesAmount = 0.00;
            var foundAtLeastOne = false;
 
            foreach (var c in com.LineItems)
                //Console.WriteLine(
                //    $"In {this.GetType().Name} and this is the FinancialConcept name: {c.Item.Name} and amount: {c.Item.Amount}");
                if (c.Item.Name.Equals("Dues"))
                {
                    foundAtLeastOne = true;
                    duesAmount = c.Item.Amount;
                    break;
                }

            var obligationUsed = AccountState.Obligations
                .FirstOrDefault(x => x.Value.Status == ObligationStatus.Active).Key;

            if (!foundAtLeastOne || string.IsNullOrEmpty(obligationUsed))
            {
                _eventsGenerated = new List<IDomainEvent>
                {
                    new AccountBusinessRuleValidationFailure(
                        AccountState.AccountNumber,
                        "AssessTaxAsPercentageOfDuesDuringBilling requires a 'Dues' amount be provided when billing."
                    )
                };
                _detailsGenerated +=
                    "AssessTaxAsPercentageOfDuesDuringBilling requires a 'Dues' amount be provided when billing.";
                Success = false;
                return;
            }


            var calculatedTaxAmount = (decimal.Round((decimal)(taxRate / 100),2) * (decimal)duesAmount) ;
            _eventsGenerated = new List<IDomainEvent>
            {
                new TaxAppliedDuringBilling(
                    AccountState.AccountNumber,
                    obligationUsed,
                    calculatedTaxAmount
                )
            };
            _detailsGenerated = $"Obligation used {obligationUsed}. Calculated tax amount is {calculatedTaxAmount}";
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

        private double ExtractParameter(string parameterName)
        {
            if (!CommandState.Parameters.ContainsKey(parameterName))
                throw new CommandStateOptionMissingException(
                    $"Rule AssessTaxAsPercentageOfDuesDuringBilling requires parameter '{parameterName}'.");
            double.TryParse((string) CommandState.Parameters[parameterName], out var value);
            return value;
        }
    }
}
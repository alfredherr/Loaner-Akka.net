using System;
using System.Collections.Generic;
using System.Linq;
using Akka;
using Akka.Persistence;
using Akka.Util.Internal;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Exceptions;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.Commands;
using Loaner.BoundedContexts.MaintenanceBilling.Events;
using Loaner.BoundedContexts.MaintenanceBilling.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Rules
{
    public class AssessTaxAsPercentageOfDuesDuringBilling : IAccountBusinessRule
    {
        /* Rule logic goes here. */
        public void RunRule(IDomainCommand command)
        {
            //Extract parameter TaxPercentageRate
            double taxRate = ExtractParameter("TaxPercentageRate");

            //Extract parameter Dues from Command
            double duesAmount = 0.00;
            var com = (BillingAssessment) command;
            foreach (var c in com.LineItems)
            {
                if (c.Item.Name.Equals("Dues"))
                {
                   duesAmount= c.Item.Amount;
                    break;
                }
            }

            if ( duesAmount != 0.00)
            {
                _eventsGenerated = new List<IDomainEvent>
                {
                    new AccountBusinessRuleValidationFailure(
                        AccountState.AccountNumber,
                        "AssessTaxAsPercentageOfDuesDuringBilling requires a 'Dues' amount be provided when billing."
                    )
                };;
                Success = false;
                return;
            }

            
            double calculatedTaxAmount = (taxRate / 100) * duesAmount;
            string obligationUsed = AccountState.Obligations
                .FirstOrDefault(x => x.Value.Status == ObligationStatus.Active).Key;
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

        private double ExtractParameter(string parameterName)
        {
            if (!CommandState.Parameters.ContainsKey(parameterName))
                throw new CommandStateOptionMissingException(
                    $"Rule AssessTaxAsPercentageOfDuesDuringBilling requires parameter '{parameterName}'.");
            Double.TryParse((string)CommandState.Parameters[parameterName], out double value);
            return value;
        }

        private AccountState AccountState { get; set; }
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated;
        private (string Command, Dictionary<string, object> Parameters) CommandState { get; set; }

        public AssessTaxAsPercentageOfDuesDuringBilling((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public AssessTaxAsPercentageOfDuesDuringBilling(AccountState accountState)
        {
            AccountState = accountState;
        }

        public void SetAccountState(AccountState state)
        {
            AccountState = state;
        }

        public void SetCallingCommandState((string Command, Dictionary<string, object > Parameters) commandState)
        {
            CommandState = commandState;
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
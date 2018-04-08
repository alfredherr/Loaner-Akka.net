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
    public class ApplyUacAfterBilling : IAccountBusinessRule
    {
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated;

        public ApplyUacAfterBilling((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public ApplyUacAfterBilling(AccountState accountState)
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
        public void RunRule(BillingAssessment command)
        { 
            
            var uac = decimal.Compare(AccountState.CurrentBalance, 0) < 0 ? (double) AccountState.CurrentBalance : 0.0;

            var obligationToUse = AccountState.Obligations
                .FirstOrDefault(x => x.Value.Status == ObligationStatus.Active).Key;
            if (string.IsNullOrEmpty(obligationToUse))
            {
                _detailsGenerated = $"No active obligations were found";
                Success = false;
            }
            if (uac != 0.0)
            {
                _eventsGenerated = new List<IDomainEvent>
                {
                    new UacAppliedAfterBilling(
                        AccountState.AccountNumber,
                        obligationToUse,
                        uac
                    )
                };
                _detailsGenerated = $"{uac} of UAC applied @ {DateTime.Now} when applying rule 'ApplyUacAfterBilling'.";
            }
            else
            {
                _eventsGenerated = new List<IDomainEvent>
                {
                    new AccountBusinessRuleValidationSuccess(
                        AccountState.AccountNumber,
                        $"No UAC was present on obligation {obligationToUse} @ {DateTime.Now} when applying rule 'ApplyUacAfterBilling'. No UAC action taken."
                    )
                };
                _detailsGenerated =
                    $"No UAC was present on obligation {obligationToUse} @ {DateTime.Now} when applying rule 'ApplyUacAfterBilling'. No UAC action taken.";
            }

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
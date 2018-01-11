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

        /* Rule logic goes here. */
        public void RunRule(IDomainCommand command)
        {
            double uac = AccountState.CurrentBalance < 0 ? AccountState.CurrentBalance : 0.0;
            if (uac != 0.0)
            {
                _eventsGenerated = new List<IDomainEvent>
                {

                    new UacAppliedAfterBilling(
                        AccountState.AccountNumber,
                        AccountState.Obligations.FirstOrDefault(x => x.Value.Status == ObligationStatus.Active).Key,
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
                       $"No UAC was present @ {DateTime.Now} when applying rule 'ApplyUacAfterBilling'. No UAC action taken."
                    )
                };
                _detailsGenerated =
                    $"No UAC was present @ {DateTime.Now} when applying rule 'ApplyUacAfterBilling'. No UAC action taken.";
            }
            
            Success = true;
        }

        private AccountState AccountState { get; set; }
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated;
        private (string Command, Dictionary<string, object> Parameters) CommandState { get; set; }

        public ApplyUacAfterBilling((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public ApplyUacAfterBilling(AccountState accountState)
        {
            AccountState = accountState;
        }

        public void SetAccountState(AccountState state)
        {
            AccountState = state;
        }

        public void SetCallingCommandState((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }
        public bool Success { get; private set; }

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
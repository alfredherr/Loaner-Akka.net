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
    public class BillingAssessmentRule : IAccountBusinessRule
    {
        private AccountState AccountState { get; set; }
        private string _detailsGenerated;
        private List<IDomainEvent> _eventsGenerated = new List<IDomainEvent>();
        private (string Command, Dictionary<string, object> Parameters) CommandState { get; set; }

        /* Rule logic goes here. */
        public void RunRule(IDomainCommand command)
        {
            //User CommandState to get list of passed in options. 
            // and COMMAND to merge the specifics of the command
            if (command is BillingAssessment billing)
                foreach (var billLine in billing.LineItems)
                {
                    _eventsGenerated.Add(new ObligationAssessedConcept(
                            AccountState.Obligations.FirstOrDefault(x => x.Value.Status == ObligationStatus.Active).Key,
                            billLine.Item
                        )
                    );
                    _detailsGenerated = "THIS WORKED";
                    Success = true;
                }
            else
            {
                throw new Exception($"I don't know how to handle commands of type {command.GetType().Name}");
            }
            
        }

        public BillingAssessmentRule()
        {
            
        }
        public BillingAssessmentRule((string Command, Dictionary<string, object> Parameters) commandState)
        {
            CommandState = commandState;
        }

        public BillingAssessmentRule(AccountState accountState)
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
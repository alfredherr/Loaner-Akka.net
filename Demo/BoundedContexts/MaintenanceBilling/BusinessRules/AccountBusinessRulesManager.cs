using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates.StateModels;
using Demo.BoundedContexts.MaintenanceBilling.BusinessRules.Exceptions;
using Demo.BoundedContexts.MaintenanceBilling.Commands;

namespace Demo.BoundedContexts.MaintenanceBilling.BusinessRules
{
    public class AccountBusinessRulesManager
    {
        public static BusinessRuleApplicationResult ApplyBusinessRules(AccountState accountState, IDomainCommand comnd)
        {
            List<IAccountBusinessRule> rules = GetBusinessRulesToApply(accountState, comnd) ?? throw new ArgumentNullException("GetBusinessRulesToApply(accountState, comnd)");
            BusinessRuleApplicationResult result = new BusinessRuleApplicationResult();

            foreach (IAccountBusinessRule reglaDeNegocio in rules)
                switch (reglaDeNegocio)
                {
                    case AccountBalanceMustNotBeNegative rule:
                        rule.RunRule();
                        if (rule.Success)
                        {
                            result.RuleProcessedResults.Add(rule, $"Business Rule Applied. {rule.GetResultDetails()}");
                            rule.GetGeneratedEvents().ForEach(@event => result.GeneratedEvents.Add(@event));
                            result.GeneratedState = rule.GetGeneratedState();
                            result.Success = true;
                        }
                        else
                        {
                            result.Success = false;
                            result.RuleProcessedResults.Add(rule,
                                $"Business Rule Failed Application. {rule.GetResultDetails()}");
                            return result; //we stop processing any further rules.
                        }
                        break;
                    case AnObligationMustBeActiveForBilling rule:
                        rule.RunRule();
                        if (rule.Success)
                        {
                            result.RuleProcessedResults.Add(rule, $"Business Rule Applied. {rule.GetResultDetails()}");
                            rule.GetGeneratedEvents().ForEach(@event => result.GeneratedEvents.Add(@event));
                            result.GeneratedState = rule.GetGeneratedState();
                            result.Success = true;
                        }
                        else
                        {
                            result.Success = false;
                            result.RuleProcessedResults.Add(rule,
                                $"Business Rule Failed Application. {rule.GetResultDetails()}");
                            return result; //we stop processing any further rules.
                        }
                        
                         
                        break;
                    default:
                        throw new UnknownBusinessRule();
                }
            //for each rule in rules
            // pass the info to the rule and call rule
            // create event of result of calling rule & apply event to state
            // return new state
            return result;
        }

        public static List<IAccountBusinessRule> GetBusinessRulesToApply(AccountState accountState,
            IDomainCommand command)
        {
            //When ApplyEvent is a SettleFinancialConcept?orWhatever command
            // get the rules to apply to this account for this particular command
            // and the order in which they need to be applied
            List<IAccountBusinessRule> list = new List<IAccountBusinessRule>
            {
                new AccountBalanceMustNotBeNegative(accountState)
            };
            
           
            if (command is BillingAssessment cmd)
            {
                list.Add(new AnObligationMustBeActiveForBilling(accountState, cmd.LineItems));
            }

            return list;
        }
    }
}
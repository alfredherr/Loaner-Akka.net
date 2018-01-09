using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.StateModels;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Exceptions;
using Loaner.BoundedContexts.MaintenanceBilling.Commands;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules
{
    public class AccountBusinessRulesManager
    {
        public static BusinessRuleApplicationResult ApplyBusinessRules(string client, string portfolioName, AccountState accountState, IDomainCommand comnd)
        {
            List<IAccountBusinessRule> rules = GetBusinessRulesToApply(client, portfolioName, accountState, comnd) ?? throw new ArgumentNullException("GetBusinessRulesToApply(accountState, comnd)");
            BusinessRuleApplicationResult result = new BusinessRuleApplicationResult();

            foreach (IAccountBusinessRule reglaDeNegocio in rules)
            {
                // I don't see a need to explicitly handle rules
                // we could remove the switch and just call directly.
                switch (reglaDeNegocio)
                {
                    case AccountBalanceMustNotBeNegative rule:
                        rule.SetAccountState(accountState);
                        rule.RunRule();
                        if (rule.RuleAppliedSuccessfuly())
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
                        rule.SetAccountState(accountState);
                        if (comnd is BillingAssessment cmd)
                        {
                            rule.SetLineItems(cmd.LineItems);
                        }
                        rule.RunRule();
                        if (rule.RuleAppliedSuccessfuly())
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
            }
            //for each rule in rules
            // pass the info to the rule and call rule
            // create event of result of calling rule & apply event to state
            // return new state
            return result;
        }

        public static List<IAccountBusinessRule> GetBusinessRulesToApply(string client, string portfolioName, AccountState accountState,
            IDomainCommand command)
        {
            //When ApplyEvent is a SettleFinancialConcept?orWhatever command
            // get the rules to apply to this account for this particular command
            // and the order in which they need to be applied
            // In future We would also want to pass in the command so we filter the search to just the rules 
            // associated to the command
            var rulesFound = BusinessRulesMap.GetAccountBusinessRulesForCommand(client, portfolioName, accountState.AccountNumber, command);
            rulesFound.ForEach(x => Console.WriteLine($"{accountState.AccountNumber} matched rule '{x.GetType().Name}' associated to command {command.GetType().Name}") );
            return BusinessRulesMap.GetAccountBusinessRulesForCommand(client, portfolioName,accountState.AccountNumber, command);
 
        }
    }
}
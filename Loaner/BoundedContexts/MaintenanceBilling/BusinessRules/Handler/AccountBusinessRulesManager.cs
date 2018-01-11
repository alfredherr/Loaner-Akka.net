using System;
using System.Collections.Generic;
using Akka.Event;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.StateModels;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Exceptions;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Rules;
using Loaner.BoundedContexts.MaintenanceBilling.Commands;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    public class AccountBusinessRulesManager
    {
        public static BusinessRuleApplicationResult ApplyBusinessRules(ILoggingAdapter log, string client, string portfolioName,
            AccountState accountState, IDomainCommand comnd)
        {
            ILoggingAdapter Log = log;

            List<IAccountBusinessRule> rules =
                GetBusinessRulesToApply(client, portfolioName, accountState, comnd) ??
                throw new ArgumentNullException($"GetBusinessRulesToApply(accountState, comnd)");

            Log.Debug($"Found {rules.Count} a total of rules for account {accountState.AccountNumber}");

            BusinessRuleApplicationResult result = new BusinessRuleApplicationResult();

            foreach (IAccountBusinessRule reglaDeNegocio in rules)
            {
                Log.Debug($"Found {reglaDeNegocio.GetType().Name} rule for account {accountState.AccountNumber}");

                reglaDeNegocio.SetAccountState(accountState);

                reglaDeNegocio.RunRule(comnd);

                if (reglaDeNegocio.RuleAppliedSuccessfuly())
                {
                    result.RuleProcessedResults.Add(reglaDeNegocio,
                        $"Business Rule Applied. {reglaDeNegocio.GetResultDetails()}");

                    reglaDeNegocio.GetGeneratedEvents().ForEach(@event => result.GeneratedEvents.Add(@event));

                    result.GeneratedState = reglaDeNegocio.GetGeneratedState();

                    result.Success = true;

                    Log.Debug($"Business Rule {reglaDeNegocio.GetType().Name} applied successfully to account {accountState.AccountNumber}");
                    
                }
                else
                {
                    result.Success = false;

                    result.RuleProcessedResults.Add(reglaDeNegocio,
                        $"Business Rule Failed Application. {reglaDeNegocio.GetResultDetails()}");

                    Log.Debug($"Business Rule {reglaDeNegocio.GetType().Name} application failed on account {accountState.AccountNumber} due to {reglaDeNegocio.GetResultDetails()}");

                    return result; //we stop processing any further rules.
                }
            }
            //for each rule in rules
            // pass the info to the rule and call rule
            // create event of result of calling rule & apply event to state
            // return new state
            return result;
        }

        public static List<IAccountBusinessRule> GetBusinessRulesToApply(string client, string portfolioName,
            AccountState accountState,
            IDomainCommand command)
        {
            //When ApplyEvent is a SettleFinancialConcept?orWhatever command
            // get the rules to apply to this account for this particular command
            // and the order in which they need to be applied
            // In future We would also want to pass in the command so we filter the search to just the rules 
            // associated to the command
            var rulesFound =
                AccountBusinessRulesMapper.GetAccountBusinessRulesForCommand(client, portfolioName, accountState.AccountNumber,
                    command);
            rulesFound.ForEach(x =>
                Console.WriteLine(
                    $"{accountState.AccountNumber} matched rule '{x.GetType().Name}' associated to command {command.GetType().Name}"));
            return rulesFound;
        }
    }
}

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Akka.Event;
    using Aggregates.Models;
    using Models;
    using DomainCommands;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class AccountBusinessRulesHandler
    {
         private readonly AccountBusinessRulesMapper _poorMansDB = new AccountBusinessRulesMapper();

        public BusinessRuleApplicationResultModel ApplyBusinessRules(ILoggingAdapter logger, string client,
            string portfolioName,
            AccountState accountState, IDomainCommand comnd)
        {
            ILoggingAdapter log = logger;

            List<IAccountBusinessRule> rules =
                GetBusinessRulesToApply(client, portfolioName, accountState, comnd) ??
                throw new ArgumentNullException($"GetBusinessRulesToApply(accountState, comnd)");

            log.Debug($"Found {rules.Count} account business rules for account {accountState.AccountNumber}");

            //if (comnd is BillingAssessment c)
            //{
            //    var buckets = c.LineItems.Aggregate("", (x, y) => $"{x} \n name:{y.Item.Name} amount:{y.Item.Amount}");
            //    log.Debug(
            //        $"{accountState.AccountNumber} has been asked to bill financial concepts {buckets}");
            //}
            //else
            //{
            //    throw new Exception($"what is {comnd.GetType().Name}");
            //}

            BusinessRuleApplicationResultModel resultModel = new BusinessRuleApplicationResultModel();

            AccountState pipedState = AccountState.Clone(accountState);

            foreach (IAccountBusinessRule reglaDeNegocio in rules)
            {
                log.Debug($"Found {reglaDeNegocio.GetType().Name} rule for account {accountState.AccountNumber}");

                reglaDeNegocio.SetAccountState(pipedState);

                reglaDeNegocio.RunRule(comnd);

                if (reglaDeNegocio.RuleAppliedSuccessfuly())
                {
                    //Save the rule and results into the return model 'resultModel'.
                    resultModel.RuleProcessedResults.Add(reglaDeNegocio,
                        $"Business Rule {reglaDeNegocio.GetType().Name} applied successfully to account {accountState.AccountNumber}. Details: {reglaDeNegocio.GetResultDetails()}");

                    //Save all the events resulting from runnin this rule.
                    var events = reglaDeNegocio.GetGeneratedEvents().ToList();
                    foreach (var @event in events)
                    {
                        resultModel.GeneratedEvents.Add(@event);
                    }
                    

                    // Rule output -> next rule input (Pipes & Filters approach)
                    // Replace pipedState wth the state resulting from running the rule.
                    pipedState = AccountState.Clone(reglaDeNegocio.GetGeneratedState());

                    resultModel.Success = true;

                    log.Debug(
                        $"Business Rule {reglaDeNegocio.GetType().Name} applied successfully to account {accountState.AccountNumber}");
                }
                else
                {
                    resultModel.Success = false;

                    resultModel.RuleProcessedResults.Add(reglaDeNegocio,
                        $"Business Rule Failed Application. {reglaDeNegocio.GetResultDetails()}");

                    log.Debug($"Business Rule {reglaDeNegocio.GetType().Name} " +
                              $"application failed on account {accountState.AccountNumber} " +
                              $"due to {reglaDeNegocio.GetResultDetails()}");

                    return resultModel; //we stop processing any further rules.
                }
            }
            //for each rule in rules
            // pass the info to the rule and call rule
            // create event of result of calling rule & apply event to state
            // return new state
            return resultModel;
        }

        private List<IAccountBusinessRule> GetBusinessRulesToApply(string client, string portfolioName,
            AccountState accountState,
            IDomainCommand command)
        {
            //When ApplyEvent is a SettleFinancialConcept?orWhatever command
            // get the rules to apply to this account for this particular command
            // and the order in which they need to be applied
            // In future We would also want to pass in the command so we filter the search to just the rules 
            // associated to the command
            var rulesFound =
                _poorMansDB.GetAccountBusinessRulesForCommand(client, portfolioName,
                    accountState.AccountNumber,
                    command);
            //rulesFound.ForEach(x =>
            //    Console.WriteLine(
            //        $"{accountState.AccountNumber} matched rule '{x.GetType().Name}' associated to command {command.GetType().Name}"));
            return rulesFound;
        }
    }
}
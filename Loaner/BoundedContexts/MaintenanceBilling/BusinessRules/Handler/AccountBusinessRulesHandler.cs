using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AccountBusinessRulesHandler: ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        private readonly AccountBusinessRulesMapper _poorMansDb = new AccountBusinessRulesMapper();

        public AccountBusinessRulesHandler()
        {
            Receive<ApplyBusinessRules>(command => ApplyBusinessRules(command));

        }

        private void ApplyBusinessRules(ApplyBusinessRules cmd)
        {
            var resultModel = new BusinessRuleApplicationResultModel();
            resultModel.TotalBilledAmount = cmd.TotalBilledAmount; //TODO what a hack! Ha!
            try
            {
              
                var rules =
                    GetBusinessRulesToApply(cmd.Client, cmd.PortfolioName, cmd.AccountState, cmd.Command) ??
                    throw new ArgumentNullException($"GetBusinessRulesToApply(accountState, cmd)");

                _logger.Debug($"Found {rules.Count} account business rules for account {cmd.AccountState.AccountNumber}");

              
                var pipedState = cmd.AccountState;
                
//                rules.ForEach(x =>
//                {
//                    var isnull = x == null ? "null" : "not null";
//
//                    Console.WriteLine($"[GetBusinessRulesToApply]: " +
//                                      $"{pipedState.AccountNumber} matched rule " +
//                                      $"'{x.GetType().Name}' associated to command {x.GetType().Name}" +
//                                      $" and isnull={isnull}");
//                });
                foreach (var reglaDeNegocio in rules)
                {

                    reglaDeNegocio.SetAccountState(pipedState);
                    reglaDeNegocio.RunRule(cmd.Command);

                    _logger.Info($"Found {reglaDeNegocio.GetType().Name} rule for account {cmd.AccountState.AccountNumber}" +
                                 $" and its Success={reglaDeNegocio.RuleAppliedSuccessfuly()}");

                    if (reglaDeNegocio.RuleAppliedSuccessfuly())
                    {
                        //Save the rule and results into the return model 'resultModel'.
                        resultModel.RuleProcessedResults.Add(reglaDeNegocio,
                            $"Business Rule {reglaDeNegocio.GetType().Name} applied successfully to account " +
                            $"{cmd.AccountState.AccountNumber}. Details: {reglaDeNegocio.GetResultDetails()}");

                        //Save all the events resulting from runnin this rule.
                        var events = reglaDeNegocio.GetGeneratedEvents().ToList();
                        foreach (var @event in events) resultModel.GeneratedEvents.Add(@event);


                        // Rule output -> next rule input (Pipes & Filters approach)
                        // Replace pipedState wth the state resulting from running the rule.
                        pipedState = (AccountState) (reglaDeNegocio.GetGeneratedState()).Clone();

                        resultModel.Success = true;

                        _logger.Debug(
                            $"Business Rule {reglaDeNegocio.GetType().Name} applied successfully to account {cmd.AccountState.AccountNumber}");
                    }
                    else
                    {
                        resultModel.Success = false;

                        resultModel.RuleProcessedResults.Add(reglaDeNegocio,
                            $"Business Rule Failed Application. {reglaDeNegocio.GetResultDetails()}");

                        _logger.Debug($"Business Rule {reglaDeNegocio.GetType().Name} " +
                                  $"application failed on account {cmd.AccountState.AccountNumber} " +
                                  $"due to {reglaDeNegocio.GetResultDetails()}");

                        Sender.Tell(resultModel);//we stop processing any further rules.
                    }
                }
               
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _logger.Error($"{e.Message} \n {e.StackTrace}");
                throw;
            }
            //for each rule in rules
            // pass the info to the rule and call rule
            // create event of result of calling rule & apply event to state
            // return new state
            Sender.Tell(resultModel);
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
            List<IAccountBusinessRule> rulesFound =
                _poorMansDb.GetAccountBusinessRulesForCommand(client, portfolioName,
                    accountState.AccountNumber,
                    command);

            return rulesFound;
        }
    }

    public class ApplyBusinessRules
    {
        public ApplyBusinessRules()
        {
            
        }
        public double TotalBilledAmount { get; set; } //TODO tis has to be moved somewhere else
        public string Client { get; set; }
        public string PortfolioName { get; set; }
        public AccountState AccountState { get; set; }
        public BillingAssessment Command { get; set; }
    }
}
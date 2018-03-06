using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using Google.Protobuf.WellKnownTypes;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AccountBusinessRulesHandler : ReceiveActor
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        public AccountBusinessRulesHandler()
        {
            Receive<BootUp>(cmd => DoBootUp(cmd));
            Receive<ApplyBusinessRules>(command => GetBusinessRulesToApply(command));
            Receive<MappedBusinessRules>(command => DoBusinessLogic(command));
            ReceiveAny(msg =>
                _logger.Error($"[ReceiveAny]: Unhandled message in {Self.Path.Name}. Message:{msg.ToString()}"));
        }

        private void DoBootUp(BootUp cmd)
        {
            _logger.Info($"{Self.Path.Name} booting up, Sir.");
        }

        private void GetBusinessRulesToApply(ApplyBusinessRules cmd)
        {
            try
            {
                cmd.AccountBusinessMapperRouter.Tell(cmd);
                //_logger.Info($"[GetBusinessRulesToApply]: Getting business rules for {cmd.AccountState.AccountNumber}.");
                //Sender.Tell($"Done, {Sender.Path.Name}. I sent it.");
            }
            catch (Exception e)
            {
                _logger.Error($"[GetBusinessRulesToApply]: {e.Message} {e.StackTrace}");
                throw;
            }
        }

        private void DoBusinessLogic(MappedBusinessRules rules)
        {
            var resultModel =
                new BusinessRuleApplicationResultModel
                {
                    TotalBilledAmount = rules.ApplyBusinessRules.TotalBilledAmount
                }; //TODO what a hack! Ha!

            try
            {
                var pipedState = rules.ApplyBusinessRules.AccountState;

                foreach (var reglaDeNegocio in rules.Rules)
                {
                    if (reglaDeNegocio == null)
                    {
                        throw new Exception("Why is this business rule null?");
                    }

                    reglaDeNegocio.SetAccountState(pipedState);
                    reglaDeNegocio.RunRule(rules.ApplyBusinessRules.Command);

                    _logger.Debug(
                        $"Found {reglaDeNegocio.GetType().Name} rule for account {rules.ApplyBusinessRules.AccountState.AccountNumber}" +
                        $" and its Success={reglaDeNegocio.RuleAppliedSuccessfuly()}");

                    if (reglaDeNegocio.RuleAppliedSuccessfuly())
                    {
                        //Save the rule and results into the return model 'resultModel'.
                        resultModel.RuleProcessedResults.Add(reglaDeNegocio,
                            $"Business Rule {reglaDeNegocio.GetType().Name} applied successfully to account " +
                            $"{rules.ApplyBusinessRules.AccountState.AccountNumber}. Details: {reglaDeNegocio.GetResultDetails()}");

                        //Save all the events resulting from runnin this rule.
                        var events = reglaDeNegocio.GetGeneratedEvents().ToList();
                        foreach (var @event in events) resultModel.GeneratedEvents.Add(@event);

                        // Rule output -> next rule input (Pipes & Filters approach)
                        // Replace pipedState wth the state resulting from running the rule.
                        pipedState = (AccountState) (reglaDeNegocio.GetGeneratedState()).Clone();

                        resultModel.Success = true;

                        _logger.Debug(
                            $"Business Rule {reglaDeNegocio.GetType().Name} applied successfully to account {rules.ApplyBusinessRules.AccountState.AccountNumber}");
                    }
                    else
                    {
                        resultModel.Success = false;

                        resultModel.RuleProcessedResults.Add(reglaDeNegocio,
                            $"Business Rule Failed Application. {reglaDeNegocio.GetResultDetails()}");

                        _logger.Error($"Business Rule {reglaDeNegocio.GetType().Name} " +
                                      $"application failed on account {rules.ApplyBusinessRules.AccountState.AccountNumber} " +
                                      $"due to {reglaDeNegocio.GetResultDetails()}");

                        Sender.Tell(resultModel); //we stop processing any further rules.
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"[DoBusinessLogic]: {e.Message} \n {e.StackTrace}");
                throw;
            }

            //for each rule in rules
            // pass the info to the rule and call rule
            // create event of result of calling rule & apply event to state
            // return new state
            Sender.Tell(resultModel);
        }
    }
}
using System.Collections.Generic;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Rules;
using Loaner.BoundedContexts.MaintenanceBilling.Commands;
using Newtonsoft.Json;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models
{
    public class AccountBusinessRuleMap
    {
        /* Client-Portfolio-Account|Command|Rules|Parameters(comma separated key value pairs) */
        [JsonConstructor]
        public AccountBusinessRuleMap(string client, string portfolio, string accountNumber, bool forAllAccounts,
            string command,
            string businessRule, string businessRuleParameters)
        {
            Client = client;
            Portfolio = portfolio;
            AccountNumber = accountNumber;
            ForAllAccounts = forAllAccounts;
            Command = validateCommandExists(command);
            BusinessRuleParameters = SplitParameters(command, businessRuleParameters);
            BusinessRule = validateBusinessRuleExists(businessRule, BusinessRuleParameters);
        }

        public AccountBusinessRuleMap(string client, string portfolio, string account, bool allAccounts, string command,
            string rule, (string Command, Dictionary<string, object> Parameters) parameters)
        {
            Client = client;
            Portfolio = portfolio;
            AccountNumber = account;
            ForAllAccounts = allAccounts;
            Command = validateCommandExists(command);
            BusinessRuleParameters = parameters;
            BusinessRule = validateBusinessRuleExists(rule, BusinessRuleParameters);
        }

        private (string Command, Dictionary<string, object> Parameters) SplitParameters(string commandName, string parameters)
        {
            Dictionary<string, object> parametros = new Dictionary<string, object>();
            if (parameters.Contains(",") || parameters.Contains("="))
            {
                var ruleparams = parameters.Split(',');
                foreach (var parameter in ruleparams)
                {
                    string[] keyVal = parameter.Split("=");
                    parametros.Add(keyVal[0], keyVal[1]);
                }
            }
            return ( commandName, parametros);
            
        }

        private IDomainCommand validateCommandExists(string command)
        {
            switch (command)
            {
                case "BillingAssessment":
                    return new BillingAssessment();
                default:
                    throw new InvalidIDomainCommandException(command);
            }
        }

        private IAccountBusinessRule validateBusinessRuleExists(string rule, (string Command, Dictionary<string, object> Parameters) parameters)
        {
            switch (rule)
            {
                // This is clearly a horrible way to do this
                // TODO: make this dynamic
                case "AccountBalanceMustNotBeNegative":
                    return new AccountBalanceMustNotBeNegative(parameters);
                case "AnObligationMustBeActiveForBilling":
                    return new AnObligationMustBeActiveForBilling(parameters);
                case "AssessTaxAsPercentageOfDuesDuringBilling":
                    return new AssessTaxAsPercentageOfDuesDuringBilling(parameters);
                case "ApplyUacAfterBilling":
                    return new ApplyUacAfterBilling(parameters);
                case "BillingConceptCannotBeBilledMoreThanOnce":
                    return new BillingConceptCannotBeBilledMoreThanOnce(parameters);
                case "ClientSpecificRuleForCalculatingTax":
                    return new ClientSpecificRuleForCalculatingTax(parameters);
                default:
                    throw new InvalidIAccountBusinessRuleException(rule);
            }
        }

        public string Client { get; private set; }
        public string Portfolio { get; private set; }
        public string AccountNumber { get; private set; }
        public bool ForAllAccounts { get; private set; }
        public IDomainCommand Command { get; private set; }
        public IAccountBusinessRule BusinessRule { get; private set; }
        public (string Command, Dictionary<string, object> Parameters) BusinessRuleParameters { get; private set; }
    }
}
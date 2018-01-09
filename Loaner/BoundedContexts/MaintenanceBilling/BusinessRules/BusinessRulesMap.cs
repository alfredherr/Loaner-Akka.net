using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using Loaner.BoundedContexts.MaintenanceBilling.Commands;
using Newtonsoft.Json;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules
{
    public class BusinessRulesMap
    {

        private static BusinessRulesMap _rulesMapInstance;

        public static List<IAccountBusinessRule> GetAccountBusinessRulesForCommand(string client, string porfolio, string accountNumber,IDomainCommand command)
        {
            if (BusinessRulesMap._rulesMapInstance == null)
            {
                Initialize();
            }

            return (_rulesMapInstance.RulesInFile
                .Where(ruleMap => 
                    // Look for rules associated to this command              
                    ruleMap.Command.GetType().Name.Equals(command.GetType().Name) &&
                                    // which also either match this account
                                    ( ruleMap.AccountNumber.Equals(accountNumber) ||
                                      // or all accounts under this portfolio
                                      ruleMap.Portfolio.Equals(porfolio) && ruleMap.ForAllAccounts ||
                                      // or all accounts under all portfolios for this client
                                      ruleMap.Client.Equals(client) && ruleMap.Portfolio.Equals("*") && ruleMap.ForAllAccounts)
                                     )
                .Select(ruleMap => ruleMap.BusinessRule)).ToList();
        }

        public static List<AccountBusinessRuleMap> ListAllAccountBusinessRules()
        {
            if (BusinessRulesMap._rulesMapInstance == null)
            {
                Initialize();
            }

            return _rulesMapInstance.RulesInFile;
        }

        public static List<AccountBusinessRuleMap> UpdateAccountBusinessRules(List<AccountBusinessRuleMap> updatedRules)
        {
            UpdateAndReInitialize(updatedRules);

            return _rulesMapInstance.RulesInFile;
        }

        public static List<CommandToBusinessRule> GetCommandsToBusinesRules()
        {
            List<CommandToBusinessRule> commands = new List<CommandToBusinessRule>();
            
            try
            {
                var filename = Environment.GetEnvironmentVariable("COMMANDS_TO_RULES_FILENAME");
                string[] readText = File.ReadAllLines(filename);
                foreach (var line in readText)
                {
                    if (line.StartsWith("#"))
                        continue;
                    var tokens = line.Split('|');
                    commands.Add(new CommandToBusinessRule() { Command = tokens[0], BusinessRule = tokens[1] });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return commands;
        }
        private static void UpdateAndReInitialize(List<AccountBusinessRuleMap> updatedRules)
        {
            try
            {
                var filename = Environment.GetEnvironmentVariable("BUSINESS_RULES_FILENAME");
                _rulesMapInstance = new BusinessRulesMap(filename, updatedRules);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void Initialize()
        {
            try
            {
                var filename = Environment.GetEnvironmentVariable("BUSINESS_RULES_FILENAME");
                _rulesMapInstance = new BusinessRulesMap(filename);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public BusinessRulesMap(string businessRulesMapFile)
        {
            RulesInFile = new List<AccountBusinessRuleMap>();
            if (!File.Exists(businessRulesMapFile))
            {
                throw new FileNotFoundException(@"I can't find {businessRulesMapFile}");
            }


            ReadInBusinessRules(businessRulesMapFile);
        }

        public BusinessRulesMap(string businessRulesMapFile, List<AccountBusinessRuleMap> updatedRules)
        {
            RulesInFile = new List<AccountBusinessRuleMap>();
            if (!File.Exists(businessRulesMapFile))
            {
                throw new FileNotFoundException(@"I can't find {businessRulesMapFile}");
            }
            WriteOutBusinessRules(businessRulesMapFile, updatedRules);
            ReadInBusinessRules(businessRulesMapFile);
        }

        private void WriteOutBusinessRules(string businessRulesMapFile, List<AccountBusinessRuleMap> updatedRules)
        {
            string[] readText = File.ReadAllLines(businessRulesMapFile);
            List<string> outfile = new List<string>();
            foreach (string s in readText)
            {
                if (s.StartsWith("#"))
                {
                    outfile.Add(s);
                }
                else
                {
                    outfile.Add($"#Removed on ({DateTime.Now})|{s}"); //comment out old rules
                }
            }
            foreach (var update in updatedRules)
            {
                string Client = update.Client;
                string Portfolio = update.Portfolio;
                string AccountNumber = update.ForAllAccounts ? "*" : update.AccountNumber;
                string Command = update.Command.GetType().Name;
                string BusinessRule = update.BusinessRule.GetType().Name;
                string BusinessRuleParameters = "";
                if (update.BusinessRuleParameters.Count == 0)
                {
                    BusinessRuleParameters = "NoParameters";
                }
                else
                {
                    foreach (var keyval in update.BusinessRuleParameters)
                    {
                        if (BusinessRuleParameters.Length == 0)
                        {
                            BusinessRuleParameters = $"{keyval.Key}={keyval.Value}";
                        }
                        else
                        {
                            BusinessRuleParameters += $",{keyval.Key}={keyval.Value}";
                        }
                    }
                }
                /* Client-Portfolio-Account|Command|Rules|Parameters(comma separated key value pairs) */

                outfile.Add($"{Client}-{Portfolio}-{AccountNumber}|{Command}|{BusinessRule}|{BusinessRuleParameters}");
            }

            try
            {
                File.WriteAllLines(businessRulesMapFile, outfile);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void ReadInBusinessRules(string businessRulesMapFile)
        {
            string[] readText = File.ReadAllLines(businessRulesMapFile);
            foreach (string s in readText)
            {
                if (s.StartsWith("#"))
                    continue;

                var tokens = s.Split('|');
                var dpl = tokens[0].Split('-');
                if (tokens.Length != 4 || dpl.Length != 3)
                {
                    throw new InvalidBusinessRulesMapFile(businessRulesMapFile);
                }
                else
                {
                    bool all = dpl[2].Contains("*");
                    Dictionary<string, string> parametros = new Dictionary<string, string>();
                    if (tokens[3].Contains(",") || tokens[3].Contains("="))
                    {
                        var ruleparams = tokens[3].Split(',');
                        foreach (var parameter in ruleparams)
                        {
                            string[] keyVal = parameter.Split("=");
                            parametros.Add(keyVal[0], keyVal[1]);
                        }
                    }

                    RulesInFile.Add(new AccountBusinessRuleMap(
                        client: dpl[0],
                        portfolio: dpl[1],
                        account: dpl[2],
                        allAccounts: all,
                        command: tokens[1],
                        rule: tokens[2],
                        parameters: parametros
                    ));
                }
                Console.WriteLine(s);
            }
        }

        List<AccountBusinessRuleMap> RulesInFile { get; }
    }

    public class CommandToBusinessRule
    {
        public string Command { get; set; }
        public string BusinessRule { get; set; }
    }

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
            BusinessRule = validateBusinessRuleExists(businessRule);
            BusinessRuleParameters = SplitParameters(businessRuleParameters);
        }

        public AccountBusinessRuleMap(string client, string portfolio, string account, bool allAccounts, string command,
            string rule, Dictionary<string, string> parameters)
        {
            Client = client;
            Portfolio = portfolio;
            AccountNumber = account;
            ForAllAccounts = allAccounts;
            Command = validateCommandExists(command);
            BusinessRule = validateBusinessRuleExists(rule);
            BusinessRuleParameters = parameters;
        }

        private Dictionary<string, string> SplitParameters(string parameters)
        {
            Dictionary<string, string> parametros = new Dictionary<string, string>();
            if (parameters.Contains(",") || parameters.Contains("="))
            {
                var ruleparams = parameters.Split(',');
                foreach (var parameter in ruleparams)
                {
                    string[] keyVal = parameter.Split("=");
                    parametros.Add(keyVal[0], keyVal[1]);
                }
            }
            return parametros;
        }

        private IDomainCommand validateCommandExists(string command)
        {
            switch (command)
            {
                case "BillingAssessment":
                    return new BillingAssessment();
                default:
                    throw new InvalidIDomainCommand(command);
            }
        }

        private IAccountBusinessRule validateBusinessRuleExists(string rule)
        {
            switch (rule)
            {
                case "AccountBalanceMustNotBeNegative":
                    return new AccountBalanceMustNotBeNegative();
                case "AnObligationMustBeActiveForBilling":
                    return new AnObligationMustBeActiveForBilling();
                default:
                    throw new InvalidIAccountBusinessRule(rule);
            }
        }

        public string Client { get; private set; }
        public string Portfolio { get; private set; }
        public string AccountNumber { get; private set; }
        public bool ForAllAccounts { get; private set; }
        public IDomainCommand Command { get; private set; }
        public IAccountBusinessRule BusinessRule { get; private set; }
        public Dictionary<string, string> BusinessRuleParameters { get; private set; }
    }

    public class InvalidIAccountBusinessRule : Exception
    {
        public InvalidIAccountBusinessRule(string rule) : base(rule)
        {
        }
    }

    public class InvalidIDomainCommand : Exception
    {
        public InvalidIDomainCommand(string command) : base(command)
        {
        }
    }

    public class InvalidBusinessRulesMapFile : Exception
    {
        public InvalidBusinessRulesMapFile(string businessRulesMapFile) : base(businessRulesMapFile)
        {
        }
    }
}
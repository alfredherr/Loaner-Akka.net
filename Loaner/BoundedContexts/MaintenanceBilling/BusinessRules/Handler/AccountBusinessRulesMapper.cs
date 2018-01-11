using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models;
using Loaner.BoundedContexts.MaintenanceBilling.Commands;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    public class AccountBusinessRulesMapper
    {

        private static AccountBusinessRulesMapper _rulesMapperInstance;

        public static List<IAccountBusinessRule> 
            GetAccountBusinessRulesForCommand(string client, string porfolio, string accountNumber,IDomainCommand command)
        {
            if (AccountBusinessRulesMapper._rulesMapperInstance == null)
            {
                Initialize();
            }

            List<IAccountBusinessRule> rulesFound = (_rulesMapperInstance.RulesInFile
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
            
            return rulesFound;
        }

        public static List<AccountBusinessRuleMap> ListAllAccountBusinessRules()
        {
            if (AccountBusinessRulesMapper._rulesMapperInstance == null)
            {
                Initialize();
            }

            return _rulesMapperInstance.RulesInFile;
        }

        public static List<AccountBusinessRuleMap> UpdateAccountBusinessRules(List<AccountBusinessRuleMap> updatedRules)
        {
            UpdateAndReInitialize(updatedRules);

            return _rulesMapperInstance.RulesInFile;
        }

        public static List<CommandToBusinessRule> GetCommandsToBusinesRules()
        {
            List<CommandToBusinessRule> commands = new List<CommandToBusinessRule>();
            
            try
            {
                var filename = Environment.GetEnvironmentVariable("COMMANDS_TO_RULES_FILENAME");
                Console.WriteLine($"COMMANDS_TO_RULES_FILENAME file location: {filename}");
                string[] readText = File.ReadAllLines(filename);
                foreach (var line in readText)
                {
                    if (line.StartsWith("#"))
                        continue;
                    var tokens = line.Split('|');
                    var param = tokens[2].Split(',');
                    Dictionary<string, string> parameters = new Dictionary<string, string>();
                    if (param.Length > 1 || tokens[2].Contains("="))
                    {
                        foreach (var p in param)
                        {
                            var keyVals = p.Split('=');
                            parameters.Add(keyVals[0], keyVals[1]);
                        }
                    }
                    commands.Add(new CommandToBusinessRule() { Command = tokens[0], BusinessRule = tokens[1], Parameters = parameters, Description = tokens[3]  });
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
                _rulesMapperInstance = new AccountBusinessRulesMapper(filename, updatedRules);
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
                _rulesMapperInstance = new AccountBusinessRulesMapper(filename);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public AccountBusinessRulesMapper(string businessRulesMapFile)
        {
            RulesInFile = new List<AccountBusinessRuleMap>();
            if (!File.Exists(businessRulesMapFile))
            {
                throw new FileNotFoundException(@"I can't find {businessRulesMapFile}");
            }
            
            ReadInBusinessRules(businessRulesMapFile);
        }

        public AccountBusinessRulesMapper(string businessRulesMapFile, List<AccountBusinessRuleMap> updatedRules)
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
                string client = update.Client;
                string portfolio = update.Portfolio;
                string accountNumber = update.ForAllAccounts ? "*" : update.AccountNumber;
                string command = update.Command.GetType().Name;
                string businessRule = update.BusinessRule.GetType().Name;
                string businessRuleParameters = "";
                if (update.BusinessRuleParameters.Parameters.Count == 0)
                {
                    businessRuleParameters = "NoParameters";
                }
                else
                {
                    foreach (var keyval in update.BusinessRuleParameters.Parameters)
                    {
                        if (businessRuleParameters.Length == 0)
                        {
                            businessRuleParameters = $"{keyval.Key}={(string)keyval.Value}";
                        }
                        else
                        {
                            businessRuleParameters += $",{keyval.Key}={(string)keyval.Value}";
                        }
                    }
                }
                /* Client-Portfolio-Account|Command|Rules|Parameters(comma separated key value pairs) */

                outfile.Add($"{client}-{portfolio}-{accountNumber}|{command}|{businessRule}|{businessRuleParameters}");
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
                    throw new InvalidBusinessRulesMapFileException(businessRulesMapFile);
                }
                else
                {
                    bool all = dpl[2].Contains("*");
                    Dictionary<string, object> parametros = new Dictionary<string, object>();
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
                        parameters: (tokens[1], parametros)
                    ));
                }
                
            }
        }

        List<AccountBusinessRuleMap> RulesInFile { get; }
    }

    public class InvalidIAccountBusinessRuleException : Exception
    {
        public InvalidIAccountBusinessRuleException(string rule) : base(rule)
        {
        }
    }

    public class InvalidIDomainCommandException : Exception
    {
        public InvalidIDomainCommandException(string command) : base(command)
        {
        }
    }

    public class InvalidBusinessRulesMapFileException : Exception
    {
        public InvalidBusinessRulesMapFileException(string businessRulesMapFile) : base(businessRulesMapFile)
        {
        }
    }
}
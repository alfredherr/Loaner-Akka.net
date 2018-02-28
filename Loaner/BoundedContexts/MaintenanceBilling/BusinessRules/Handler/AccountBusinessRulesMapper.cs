using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Loaner.ActorManagement;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Exceptions;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Rules;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;

namespace Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler
{
    using static LoanerActors;

    public class AccountBusinessRulesMapper
    {
        private AccountBusinessRulesMapper _rulesMapperInstance;

        public AccountBusinessRulesMapper()
        {
            Initialize();
        }

        public AccountBusinessRulesMapper(string businessRulesMapFile)
        {
            RulesInFile = new List<AccountBusinessRuleMapModel>();
            if (!File.Exists(businessRulesMapFile))
                throw new FileNotFoundException($"I can't find {businessRulesMapFile}");

            ReadInBusinessRules(businessRulesMapFile);
        }

        public AccountBusinessRulesMapper(string businessRulesMapFile, List<AccountBusinessRuleMapModel> updatedRules)
        {
            RulesInFile = new List<AccountBusinessRuleMapModel>();
            if (!File.Exists(businessRulesMapFile))
                throw new FileNotFoundException($"I can't find {businessRulesMapFile}");
            WriteOutBusinessRules(businessRulesMapFile, updatedRules);
            ReadInBusinessRules(businessRulesMapFile);
        }

        private List<AccountBusinessRuleMapModel> RulesInFile { get; }

        public List<IAccountBusinessRule>
            GetAccountBusinessRulesForCommand(string client, string porfolio, string accountNumber,
                IDomainCommand command)
        {
            if (_rulesMapperInstance == null) Initialize();

            var rules = _rulesMapperInstance.RulesInFile
                .Where(ruleMap =>
                    // Look for rules associated to this command              
                        ruleMap.Command.GetType().Name.Equals(command.GetType().Name) &&
                        // which also either match this account
                        (ruleMap.AccountNumber.Equals(accountNumber) ||
                         // or all accounts under this portfolio
                         ruleMap.Portfolio.Equals(porfolio) && ruleMap.ForAllAccounts ||
                         // or all accounts under all portfolios for this client
                         ruleMap.Client.Equals(client) && ruleMap.Portfolio.Equals("*") && ruleMap.ForAllAccounts)
                )
                .Select(ruleMap => ruleMap.BusinessRule).ToImmutableList();

            var rulesFound = new List<IAccountBusinessRule>();

            // TODO need to make this dynamic using replection
            //And lastly add the command rule itself
            rulesFound.Add(new BillingAssessmentRule());

            rulesFound.AddRange(rules.ToList());

            return rulesFound;
        }

        public List<AccountBusinessRuleMapModel> ListAllAccountBusinessRules()
        {
            if (_rulesMapperInstance == null) Initialize();

            return _rulesMapperInstance.RulesInFile;
        }

        public List<AccountBusinessRuleMapModel> UpdateAccountBusinessRules(
            List<AccountBusinessRuleMapModel> updatedRules)
        {
            UpdateAndReInitialize(updatedRules);

            return _rulesMapperInstance.RulesInFile;
        }

        public List<CommandToBusinessRuleModel> GetCommandsToBusinesRules()
        {
            var commands = new List<CommandToBusinessRuleModel>();

            try
            {
                var filename = CommandsToRulesFilename;
                Console.WriteLine($"COMMANDS_TO_RULES_FILENAME file location: {filename}");
                var readText = File.ReadAllLines(filename);
                foreach (var line in readText)
                {
                    if (line.StartsWith("#"))
                        continue;
                    var tokens = line.Split('|');
                    var param = tokens[2].Split(',');
                    var parameters = new Dictionary<string, string>();
                    if (param.Length > 1 || tokens[2].Contains("="))
                        foreach (var p in param)
                        {
                            var keyVals = p.Split('=');
                            parameters.Add(keyVals[0], keyVals[1]);
                        }

                    commands.Add(new CommandToBusinessRuleModel
                    {
                        Command = tokens[0],
                        BusinessRule = tokens[1],
                        Parameters = parameters,
                        Description = tokens[3]
                    });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return commands;
        }

        private void UpdateAndReInitialize(List<AccountBusinessRuleMapModel> updatedRules)
        {
            try
            {
                var filename = BusinessRulesFilename;
                _rulesMapperInstance = new AccountBusinessRulesMapper(filename, updatedRules);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void Initialize()
        {
            try
            {
                var filename = BusinessRulesFilename;
                _rulesMapperInstance = new AccountBusinessRulesMapper(filename);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void WriteOutBusinessRules(string businessRulesMapFile, List<AccountBusinessRuleMapModel> updatedRules)
        {
            var readText = File.ReadAllLines(businessRulesMapFile);
            var outfile = new List<string>();
            foreach (var s in readText)
                if (s.StartsWith("#"))
                    outfile.Add(s);
                else
                    outfile.Add($"#Removed on ({DateTime.Now})|{s}"); //comment out old rules
            foreach (var update in updatedRules)
            {
                var client = update.Client;
                var portfolio = update.Portfolio;
                var accountNumber = update.ForAllAccounts ? "*" : update.AccountNumber;
                var command = update.Command.GetType().Name;
                var businessRule = update.BusinessRule.GetType().Name;
                var businessRuleParameters = "";
                if (update.BusinessRuleParameters.Parameters.Count == 0)
                    businessRuleParameters = "NoParameters";
                else
                    foreach (var keyval in update.BusinessRuleParameters.Parameters)
                        if (businessRuleParameters.Length == 0)
                            businessRuleParameters = $"{keyval.Key}={(string) keyval.Value}";
                        else
                            businessRuleParameters += $",{keyval.Key}={(string) keyval.Value}";
                /* Client-Portfolio-Account|Command|Rules|Parameters(comma separated key value pairs) */

                outfile.Add(
                    $"{client}-{portfolio.ToUpper()}-{accountNumber}|{command}|{businessRule}|{businessRuleParameters}");
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
            var readText = File.ReadAllLines(businessRulesMapFile);
            foreach (var s in readText)
            {
                if (s.Trim().StartsWith("#") || s.Trim().Length == 0 || !s.Contains("|"))
                    continue;

                var tokens = s.Split('|');
                var dpl = tokens[0].Split('-');
                if (tokens.Length != 4 || dpl.Length != 3)
                    throw new InvalidBusinessRulesMapFileException(businessRulesMapFile);

                var all = dpl[2].Contains("*");
                var parametros = new Dictionary<string, object>();
                if (tokens[3].Contains(",") || tokens[3].Contains("="))
                {
                    var ruleparams = tokens[3].Split(',');
                    foreach (var parameter in ruleparams)
                    {
                        var keyVal = parameter.Split("=");
                        parametros.Add(keyVal[0], keyVal[1]);
                    }
                }

                RulesInFile.Add(new AccountBusinessRuleMapModel(
                    dpl[0],
                    dpl[1].ToUpper(),
                    dpl[2],
                    all,
                    tokens[1],
                    tokens[2],
                    (tokens[1], parametros)
                ));
            }
        }
    }
}
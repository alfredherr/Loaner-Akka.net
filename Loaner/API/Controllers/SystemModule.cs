using System.Collections.Generic;
using System.IO;
using Loaner.API.Models;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules;
using Newtonsoft.Json;

namespace Loaner.api.Controllers
{
    using System.Linq;
    using Akka.Util.Internal;
    using BoundedContexts.MaintenanceBilling.Aggregates.Messages;
    using BoundedContexts.MaintenanceBilling.Models;
    using static ActorManagement.LoanerActors;
    using Akka.Actor;
    using BoundedContexts.MaintenanceBilling.Commands;
    using BoundedContexts.MaintenanceBilling.Events;
    using Models;
    using Nancy;
    using Nancy.ModelBinding;
    using System;
    using System.Threading.Tasks;

    public class SystemModule : NancyModule
    {
        public SystemModule() : base("/api/system")
        {
            Get("/", async args =>
            {
                var answer = new MySystemStatus("This didn't work");
                await Task.Run(() =>
                {
                    answer = DemoSystemSupervisor
                            .Ask<MySystemStatus>(new TellMeYourStatus(), TimeSpan.FromSeconds(30))
                            .Result;
                    return Response.AsJson( new SupervisedPortfolios(answer.Message, answer.Portfolios));
                });
                return Response.AsJson( new SupervisedPortfolios(answer.Message, answer.Portfolios));
            });

            Get("/run", async args =>
            {
                var answer = new MySystemStatus("This didn't work");
                await Task.Run(() =>
                {
                    answer = DemoSystemSupervisor
                        .Ask<MySystemStatus>(new StartPortfolios(), TimeSpan.FromSeconds(30))
                        .Result;
                    return Response.AsJson(new SupervisedAccounts(answer.Message, answer.Portfolios));
                });
                return Response.AsJson(answer);

            });
            Get("/businessrules", async args =>
            {
                 
                 return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => typeof(IAccountBusinessRule).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
                .Select(x => x.Name).ToList();
                
                //var rules = BusinessRulesMap.ListAllAccountBusinessRules();
                //var model = new BusinessRulesMapModel() { Message = $"Info as of: {DateTime.Now}", RulesMap = rules };
                //return rules;

            });

            Post("/businessrules", async args =>
            {
                var reader = new StreamReader(this.Request.Body);
                string text = reader.ReadToEnd();
                var newRules = JsonConvert.DeserializeObject<AccountBusinessRuleMap[]>(text);
                 
                var proof = BusinessRulesMap.UpdateAccountBusinessRules(updatedRules: newRules.ToList());
                 
                return new BusinessRulesMapModel() { Message = $"Info as of: {DateTime.Now}", RulesMap = proof };

            });

            Post("/billall", args =>
            {
                InvoiceLineItem[] assessment = this.Bind<InvoiceLineItem[]>();
                
                  Console.WriteLine($"Assessment is {assessment}");
                
               
                    assessment.ForEach(x =>
                    {
                        Console.Write($"Item Name: {x.Item.Name} \t");
                        Console.Write($"Item Amount: {x.Item.Amount} \t");
                        Console.WriteLine($"Units: {x.Units} \t");
                    });
               

                DemoActorSystem.ActorSelection($"/user/demoSupervisor/*")
                     .Tell(new AssessWholePortfolio("AllPortfolios", assessment.ToList()));
                
                return Response.AsJson(new SupervisedPortfolios("Sent billing command to all accounts", null ));
                 
            });
            
            Get("/BillingStatus", async args =>
            {
                BillingStatusModel billingSummary = null;
                await Task.Run(() =>
                {
                    var answer = DemoSystemSupervisor
                        .Ask<PortfolioBillingStatus>(new ReportBillingProgress(), TimeSpan.FromSeconds(30))
                        .Result;
                    billingSummary = new BillingStatusModel(answer);
                    billingSummary.Summarize();
                    Console.WriteLine($"Responded to API with {billingSummary.AccountsBilled} accounts billed");
                    Console.WriteLine($"Responded to API with ${billingSummary.AmountBilled} billed");
                    Console.WriteLine($"Responded to API with ${billingSummary.BalanceAfterBilling} ending balance");
                });
                return Response.AsJson( billingSummary ?? new BillingStatusModel());
            });
            
            Post("/simulation", args=>
            {
                SimulateBoardingOfAccountModel client = this.Bind<SimulateBoardingOfAccountModel>();
                
                Console.WriteLine($"Supervisor's name is: {DemoSystemSupervisor.Path.Name}");

                DemoSystemSupervisor.Tell(new SimulateBoardingOfAccounts(
                    client.ClientName,
                    client.ClientAccountsFilePath,
                    client.ObligationsFilePath
                ));

                return Response.AsJson(new MySystemStatus("Done"));
            });
            
        }
    }
}

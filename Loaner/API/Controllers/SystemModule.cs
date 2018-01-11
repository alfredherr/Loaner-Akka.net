using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Util.Internal;
using Loaner.ActorManagement;
using Loaner.API.Models;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler.Models;
using Loaner.BoundedContexts.MaintenanceBilling.Commands;
using Loaner.BoundedContexts.MaintenanceBilling.Events;
using Loaner.BoundedContexts.MaintenanceBilling.Models;
using Nancy;
using Nancy.ModelBinding;
using Newtonsoft.Json;

namespace Loaner.API.Controllers
{
    public class SystemModule : NancyModule
    {
        public SystemModule() : base("/api/system")
        {
            Get("/", async args =>
            {
                var answer = new MySystemStatus("This didn't work");
                await Task.Run(() =>
                {
                    answer = LoanerActors.DemoSystemSupervisor
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
                    answer = LoanerActors.DemoSystemSupervisor
                        .Ask<MySystemStatus>(new StartPortfolios(), TimeSpan.FromSeconds(30))
                        .Result;
                    return Response.AsJson(new SupervisedAccounts(answer.Message, answer.Portfolios));
                });
                return Response.AsJson(answer);

            });
            Get("/businessrules", async args =>
            {
                 
               return AccountBusinessRulesMapper.GetCommandsToBusinesRules();

            });

            Post("/businessrules", async args =>
            {
                var reader = new StreamReader(this.Request.Body);
                string text = reader.ReadToEnd();
                var newRules = JsonConvert.DeserializeObject<AccountBusinessRuleMapModel[]>(text);
                 
                var proof = AccountBusinessRulesMapper.UpdateAccountBusinessRules(updatedRules: newRules.ToList());
                 
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
                        
                    });
               

                LoanerActors.DemoActorSystem.ActorSelection($"/user/demoSupervisor/*")
                     .Tell(new AssessWholePortfolio("AllPortfolios", assessment.ToList()));
                
                return Response.AsJson(new SupervisedPortfolios("Sent billing command to all accounts", null ));
                 
            });
            
            Get("/BillingStatus", async args =>
            {
                BillingStatusModel billingSummary = null;
                await Task.Run(() =>
                {
                    var answer = LoanerActors.DemoSystemSupervisor
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
                
                Console.WriteLine($"Supervisor's name is: {LoanerActors.DemoSystemSupervisor.Path.Name}");

                LoanerActors.DemoSystemSupervisor.Tell(new SimulateBoardingOfAccounts(
                    client.ClientName,
                    client.ClientAccountsFilePath,
                    client.ObligationsFilePath
                ));

                return Response.AsJson(new MySystemStatus("Done"));
            });
            
        }
    }
}

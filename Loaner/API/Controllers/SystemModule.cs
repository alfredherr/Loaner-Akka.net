using System;
using System.Collections.Generic;
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
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;
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
                    return Response.AsJson(new SupervisedPortfolios(answer.Message, answer.Portfolios));
                });
                return Response.AsJson(new SupervisedPortfolios(answer.Message, answer.Portfolios));
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
                var answer = new List<CommandToBusinessRuleModel>();
                await Task.Run(() =>
                {
                    answer = LoanerActors.AccountBusinessRulesMapperRouter
                        .Ask<List<CommandToBusinessRuleModel>>(new GetCommandsToBusinesRules(),
                            TimeSpan.FromSeconds(30))
                        .Result;
                });
                return Response.AsJson(answer);
            });

            Post("/businessrules", async args =>
            {
                var reader = new StreamReader(Request.Body);
                var text = reader.ReadToEnd();
                var newRules = JsonConvert.DeserializeObject<AccountBusinessRuleMapModel[]>(text);

                var proof = new List<AccountBusinessRuleMapModel>();
                await Task.Run(() =>
                {
                    proof = LoanerActors.AccountBusinessRulesMapperRouter
                        .Ask<List<AccountBusinessRuleMapModel>>(new UpdateAccountBusinessRules(newRules.ToList()),
                            TimeSpan.FromSeconds(30))
                        .Result;
                });
                return new BusinessRulesMapModel {Message = $"Info as of: {DateTime.Now}", RulesMap = proof};
            });

            Post("/billall", args =>
            {
                var assessment = this.Bind<InvoiceLineItem[]>();

                Console.WriteLine($"Assessment is {assessment}");

                assessment.ForEach(x =>
                {
                    Console.Write($"Item Name: {x.Item.Name} \t");
                    Console.Write($"Item Amount: {x.Item.Amount} \t");
                });

                LoanerActors.DemoActorSystem.ActorSelection($"/user/demoSupervisor/*")
                    .Tell(new AssessWholePortfolio("AllPortfolios", assessment.ToList()));

                return Response.AsJson(new SupervisedPortfolios("Sent billing command to all accounts", null));
            });

            Get("/BillingStatus", async args =>
            {
                BillingStatusModel billingSummary = null;
                PortfolioBillingStatus portfolioBillingStatus = null;
                await Task.Run(() =>
                {
                    portfolioBillingStatus = LoanerActors.DemoSystemSupervisor
                        .Ask<PortfolioBillingStatus>(new ReportBillingProgress(), TimeSpan.FromSeconds(30))
                        .Result;
                });
                if (portfolioBillingStatus == null) return Response.AsJson(new BillingStatusModel());
                billingSummary = new BillingStatusModel(portfolioBillingStatus);
                billingSummary.Summarize();
                Console.WriteLine($"Responded to API with {billingSummary.AccountsBilled} accounts billed");
                Console.WriteLine(
                    $"Responded to API with {billingSummary.AmountBilled:C} billed");
                Console.WriteLine(
                    $"Responded to API with {billingSummary.BalanceAfterBilling:C} ending balance");
                return Response.AsJson(billingSummary);
            });

            Post("/simulation", args =>
            {
                var client = this.Bind<SimulateBoardingOfAccountModel>();

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
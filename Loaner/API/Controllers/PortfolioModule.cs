using System.Dynamic;
using System.Linq;
using Hyperion.Extensions;

namespace Loaner.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Akka.Actor;
    using ActorManagement;
    using Models;
    using BoundedContexts.MaintenanceBilling.Aggregates.Messages;
    using BoundedContexts.MaintenanceBilling.DomainCommands;
    using BoundedContexts.MaintenanceBilling.DomainModels;
    using Nancy;
    using Nancy.ModelBinding;


    public class PortfolioModule : NancyModule
    {
        public PortfolioModule() : base("/api/portfolio")
        {
            Get("/{portfolioName}", async args =>
            {
                var answer = new TellMeYourPortfolioStatus("This didn't work");

                string portfolio = ((string) args.portfolioName).ToUpper();

                var portfolioActor = LoanerActors.DemoActorSystem
                    .ActorSelection($"/user/demoSupervisor/{portfolio}")
                    .ResolveOne(TimeSpan.FromSeconds(3));

                if (portfolioActor.Exception != null)
                {
                    throw portfolioActor.Exception;
                }

                await Task.Run(() =>
                {
                    answer = portfolioActor.Result
                        .Ask<TellMeYourPortfolioStatus>(new TellMeYourStatus(), TimeSpan.FromSeconds(30)).Result;
                    return Response.AsJson(new SupervisedAccounts(answer.Message, answer.Accounts));
                });
                return Response.AsJson(new SupervisedAccounts(answer.Message, answer.Accounts));
            });

            Get("/{portfolioName}/run", async args =>
            {
                var answer = new TellMeYourPortfolioStatus("This didn't work");

                string portfolio = ((string) args.portfolioName).ToUpper();

                var portfolioActor = LoanerActors.DemoActorSystem
                    .ActorSelection($"/user/demoSupervisor/{portfolio}")
                    .ResolveOne(TimeSpan.FromSeconds(3));

                if (portfolioActor.Exception != null)
                {
                    throw portfolioActor.Exception;
                }

                await Task.Run(() =>
                {
                    answer = portfolioActor.Result
                        .Ask<TellMeYourPortfolioStatus>(new StartAccounts(), TimeSpan.FromSeconds(30)).Result;
                    return Response.AsJson(new SupervisedAccounts(answer.Message, answer.Accounts));
                });
                return Response.AsJson(answer);
            });

            Get("/{actorName}/assessment", args =>
            {
                var model = new SimulateAssessmentModel();
                model.LineItems = new List<InvoiceLineItem>
                {
                    new InvoiceLineItem(new Tax(0)),
                    new InvoiceLineItem(new Dues(0)),
                    new InvoiceLineItem(new Reserve(0))
                };
                return model;
            });

        
            Post("/{portfolioName}/assessment", async args =>
            {
                string portfolio = ((string) args.portfolioName).ToUpper();
                
                var answer = new TellMeYourPortfolioStatus("This didn't work");

                dynamic product = Context.ToDynamic();

                string message = string.Empty;

                SimulateAssessmentModel assessment = new SimulateAssessmentModel();
                assessment.LineItems = new List<InvoiceLineItem>();
                
                foreach (var p in product)
                {
                    string name = (string)(p.item.name ?? string.Empty);
                    double bucketAmount = (double)(p.item.amount ?? -1.0);
                    if (bucketAmount >= 0)
                    {
                        switch (name)
                        {
                            case "Dues":
                                assessment.LineItems.Add(new InvoiceLineItem(new Dues(amount: bucketAmount)));
                                break;
                            case "Tax":
                                assessment.LineItems.Add(new InvoiceLineItem(new Tax(amount: bucketAmount)));
                                break;
                            case "Reserve":
                                assessment.LineItems.Add(new InvoiceLineItem(new Reserve(amount: bucketAmount)));
                                break;
                            case "Interest":
                                assessment.LineItems.Add(new InvoiceLineItem(new Interest(amount: bucketAmount)));
                                break;
                        }
                    }
                    else
                    {
                        return Response.AsJson(new
                        {
                            Error = "You must provide a valid bucket type (i.e. Dues, Tax, etc.) and amount"
                        });
                    }
                }

                var portfolioActor = LoanerActors.DemoActorSystem
                    .ActorSelection($"/user/demoSupervisor/{portfolio}")
                    .ResolveOne(TimeSpan.FromSeconds(3));

                if (portfolioActor.Exception != null)
                {
                    throw portfolioActor.Exception;
                }

                await Task.Run(() =>
                {
                    answer = portfolioActor
                        .Result
                        .Ask<TellMeYourPortfolioStatus>(new AssessWholePortfolio(portfolio, assessment.LineItems),
                            TimeSpan.FromSeconds(5))
                        .Result;
                    return Response.AsJson(new SupervisedAccounts(answer.Message, answer.Accounts));
                });
                return Response.AsJson(answer);
                
            });
        }
       
    }

}
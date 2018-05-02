using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Loaner.ActorManagement;
using Loaner.API.Models;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;
using Nancy;

namespace Loaner.API.Controllers
{
    public class PortfolioModule : NancyModule
    {
        public PortfolioModule() : base("/api/portfolio")
        {
            Get("/{portfolioName}", async args =>
            {
                TellMeYourPortfolioStatus answer = new TellMeYourPortfolioStatus("This didn't work");

                string portfolio = ((string) args.portfolioName).ToUpper();

                Task<IActorRef> portfolioActor = LoanerActors.DemoActorSystem
                    .ActorSelection($"/user/demoSupervisor/{portfolio}")
                    .ResolveOne(TimeSpan.FromSeconds(30));

                if (portfolioActor.Exception != null) throw portfolioActor.Exception;

                await Task.Run(() =>
                {
                    answer = portfolioActor.Result
                        .Ask<TellMeYourPortfolioStatus>(new TellMeYourStatus(), TimeSpan.FromSeconds(150)).Result;
                    return Response.AsJson(new {answer.Message, PortfolioState = answer.PortfolioStateViewModel});
                });
                return Response.AsJson(new {answer.Message, PortfolioState = answer.PortfolioStateViewModel});
            });
            Get("/{portfolioName}/failedbilling", async args =>
            {
                FailedListOfAccounts answer;

                string portfolio = ((string) args.portfolioName).ToUpper();

                Task<IActorRef> portfolioActor = LoanerActors.DemoActorSystem
                    .ActorSelection($"/user/demoSupervisor/{portfolio}")
                    .ResolveOne(TimeSpan.FromSeconds(30));

                if (portfolioActor.Exception != null) throw portfolioActor.Exception;

                await Task.Run(() =>
                {
                    answer = portfolioActor.Result
                        .Ask<FailedListOfAccounts>(new GetFailedBilledAccounts(), TimeSpan.FromSeconds(150)).Result;
                    return Response.AsJson(answer);
                });
                return Response.AsJson( new FailedListOfAccounts());
            });

            Get("/{portfolioName}/run", async args =>
            {
                TellMeYourPortfolioStatus answer = new TellMeYourPortfolioStatus("This didn't work");

                string portfolio = ((string) args.portfolioName).ToUpper();

                Task<IActorRef> portfolioActor = LoanerActors.DemoActorSystem
                    .ActorSelection($"/user/demoSupervisor/{portfolio}")
                    .ResolveOne(TimeSpan.FromSeconds(3));

                if (portfolioActor.Exception != null) throw portfolioActor.Exception;

                await Task.Run(() =>
                {
                    answer = portfolioActor.Result
                        .Ask<TellMeYourPortfolioStatus>(new StartAccounts(), TimeSpan.FromSeconds(30)).Result;
                    return Response.AsJson(new {answer.Message, PortfolioState = answer.PortfolioStateViewModel});
                });
                return Response.AsJson(answer);
            });

            Get("/{portfolioName}/assessment", args =>
            {
                SimulateAssessmentModel model = new SimulateAssessmentModel();
                model.LineItems = new List<InvoiceLineItem>
                {
                    new InvoiceLineItem(new Tax(0)),
                    new InvoiceLineItem(new Dues(0)),
                    new InvoiceLineItem(new Reserve(0))
                };
                return Response.AsJson(model);
            });


            Post("/{portfolioName}/assessment", async args =>
            {
                string portfolio = ((string) args.portfolioName).ToUpper();

                TellMeYourPortfolioStatus answer = new TellMeYourPortfolioStatus("This didn't work");

                dynamic product = Context.ToDynamic();

                string message = string.Empty;

                SimulateAssessmentModel assessment = new SimulateAssessmentModel();
                assessment.LineItems = new List<InvoiceLineItem>();

                foreach (dynamic p in product)
                {
                    string name = (string) (p.item.name ?? string.Empty);
                    double bucketAmount = (double) (p.item.amount ?? -1.0);
                    if (bucketAmount >= 0)
                        switch (name)
                        {
                            case "Dues":
                                assessment.LineItems.Add(new InvoiceLineItem(new Dues(bucketAmount)));
                                break;
                            case "Tax":
                                assessment.LineItems.Add(new InvoiceLineItem(new Tax(bucketAmount)));
                                break;
                            case "Reserve":
                                assessment.LineItems.Add(new InvoiceLineItem(new Reserve(bucketAmount)));
                                break;
                            case "Interest":
                                assessment.LineItems.Add(new InvoiceLineItem(new Interest(bucketAmount)));
                                break;
                        }
                    else
                        return Response.AsJson(new
                        {
                            Error = "You must provide a valid bucket type (i.e. Dues, Tax, etc.) and amount"
                        });
                }

                IActorRef portfolioActor = await  LoanerActors.DemoActorSystem
                    .ActorSelection($"/user/demoSupervisor/{portfolio}")
                    .ResolveOne(TimeSpan.FromSeconds(10));

                //if (portfolioActor.Exception != null) throw portfolioActor.Exception;

                await Task.Run(() =>
                {
                    answer = portfolioActor
                        .Ask<TellMeYourPortfolioStatus>(new AssessWholePortfolio(portfolio, assessment.LineItems),
                            TimeSpan.FromSeconds(50))
                        .Result;
                    return Response.AsJson(new {answer.Message, PortfolioState = answer.PortfolioStateViewModel});
                });
                return Response.AsJson(answer);
            });
        }
    }
}
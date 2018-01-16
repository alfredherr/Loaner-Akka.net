using System;
using System.Threading.Tasks;
using Akka.Actor;
using Loaner.ActorManagement;
using Loaner.API.Models;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainEvents;
using Nancy;
using Nancy.ModelBinding;

namespace Loaner.API.Controllers
{
    public class PortfolioModule : NancyModule
    {
        public PortfolioModule() : base("/api/portfolio")
        {
            Get("/{portfolioName}", async args =>
            {
                var answer = new TellMeYourPortfolioStatus("This didn't work");
                
                string portfolio = ((string)args.portfolioName).ToUpper();

                var portfolioActor = LoanerActors.DemoActorSystem
                    .ActorSelection($"/user/demoSupervisor/{portfolio}")
                    .ResolveOne(TimeSpan.FromSeconds(3));
                
                if (portfolioActor.Exception != null)
                {
                    throw portfolioActor.Exception;
                }
               
                await Task.Run(() =>
                {
                    answer = portfolioActor.Result.Ask<TellMeYourPortfolioStatus>(new TellMeYourStatus(), TimeSpan.FromSeconds(30)).Result;
                    return Response.AsJson( new SupervisedAccounts(answer.Message, answer.Accounts));
                });
                return Response.AsJson( new SupervisedAccounts(answer.Message, answer.Accounts));
            });

            Get("/{portfolioName}/run", async args =>
            {
                var answer = new TellMeYourPortfolioStatus("This didn't work");
                
                string portfolio = ((string)args.portfolioName).ToUpper();

                var portfolioActor = LoanerActors.DemoActorSystem
                    .ActorSelection($"/user/demoSupervisor/{portfolio}")
                    .ResolveOne(TimeSpan.FromSeconds(3));
               
                if (portfolioActor.Exception != null)
                {
                    throw portfolioActor.Exception;
                }
               
                await Task.Run(() =>
                {
                    answer = portfolioActor.Result.Ask<TellMeYourPortfolioStatus>(new StartAccounts(), TimeSpan.FromSeconds(30)).Result;
                    return Response.AsJson(new SupervisedAccounts(answer.Message, answer.Accounts));
                });
                return Response.AsJson(answer);

            });
            Post("/{portfolioName}/assessment", async args =>
            {
                string portfolio = ((string)args.portfolioName).ToUpper();
                portfolio =portfolio.Replace("portfolio", "Portfolio");
                Console.WriteLine($"Porfolio being billed: {portfolio}");
                    
                var answer = new TellMeYourPortfolioStatus("This didn't work");
                
                SimulateAssessmentModel assessment = this.Bind<SimulateAssessmentModel>();

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
                        .Ask<TellMeYourPortfolioStatus>(new AssessWholePortfolio(portfolio,assessment.LineItems), TimeSpan.FromSeconds(5))
                        .Result;
                    return Response.AsJson(new SupervisedAccounts(answer.Message, answer.Accounts));
                });
                return Response.AsJson(answer);

            });

            
        }
    }
}
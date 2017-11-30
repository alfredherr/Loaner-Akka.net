

namespace Loaner.api.Controllers
{
    using System;
    using System.Threading.Tasks;
    using Akka.Actor;
    using static ActorManagement.LoanerActors;
    using Models;
    using BoundedContexts.MaintenanceBilling.Commands;
    using BoundedContexts.MaintenanceBilling.Events;
    using Nancy;
    using Nancy.ModelBinding;
    public class PortfolioModule : NancyModule
    {
        public PortfolioModule() : base("/api/portfolio")
        {
            Get("/{portfolioName}", async args =>
            {
                var answer = new MyPortfolioStatus("This didn't work");
                
                string portfolio = args.portfolioName;
                
                var portfolioActor = DemoActorSystem
                    .ActorSelection($"/user/demoSupervisor/{portfolio}")
                    .ResolveOne(TimeSpan.FromSeconds(3));
                
                if (portfolioActor.Exception != null)
                {
                    throw portfolioActor.Exception;
                }
               
                await Task.Run(() =>
                {
                    answer = portfolioActor.Result.Ask<MyPortfolioStatus>(new TellMeYourStatus(), TimeSpan.FromSeconds(30)).Result;
                    return Response.AsJson( new SupervisedAccounts(answer.Message, answer.Accounts));
                });
                return Response.AsJson( new SupervisedAccounts(answer.Message, answer.Accounts));
            });

            Get("/{portfolioName}/run", async args =>
            {
                var answer = new MyPortfolioStatus("This didn't work");
                
                string portfolio = args.portfolioName;
                
                var portfolioActor = DemoActorSystem
                    .ActorSelection($"/user/demoSupervisor/{portfolio}")
                    .ResolveOne(TimeSpan.FromSeconds(3));
               
                if (portfolioActor.Exception != null)
                {
                    throw portfolioActor.Exception;
                }
               
                await Task.Run(() =>
                {
                    answer = portfolioActor.Result.Ask<MyPortfolioStatus>(new StartAccounts(), TimeSpan.FromSeconds(30)).Result;
                    return Response.AsJson(new SupervisedAccounts(answer.Message, answer.Accounts));
                });
                return Response.AsJson(answer);

            });
            Post("/{portfolioName}/assessment", async args =>
            {
                string portfolio = args.portfolioName;
                    
                var answer = new MyPortfolioStatus("This didn't work");
                
                SimulateAssessmentModel assessment = this.Bind<SimulateAssessmentModel>();

                var portfolioActor = DemoActorSystem
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
                        .Ask<MyPortfolioStatus>(new AssessWholePortfolio(portfolio,assessment.LineItems), TimeSpan.FromSeconds(5))
                        .Result;
                    return Response.AsJson(new SupervisedAccounts(answer.Message, answer.Accounts));
                });
                return Response.AsJson(answer);

            });

            
        }
    }
}
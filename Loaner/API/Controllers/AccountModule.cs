using System;
using System.Threading.Tasks;
using Akka.Actor;
using Loaner.ActorManagement;
using Loaner.API.Models;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Loaner.BoundedContexts.MaintenanceBilling.DomainCommands;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels;
using Nancy;
using Nancy.ModelBinding;

namespace Loaner.API.Controllers
{
    public class AccountModule : NancyModule
    {
        public AccountModule() : base("/api/account")
        {
            Get("/{actorName}", async args =>
            {
                try
                {
                    var system = LoanerActors.DemoActorSystem
                        .ActorSelection($"/user/demoSupervisor/*/{args.actorName}")
                        .ResolveOne(TimeSpan.FromSeconds(3));
                    if (system.Exception != null) throw system.Exception;
                    var response = await Task.Run(
                        () => system.Result.Ask<MyAccountStatus>(new TellMeYourStatus(), TimeSpan.FromSeconds(1))
                    );
                    return Response.AsJson(response.Message);
                }
                catch (ActorNotFoundException)
                {
                    return new AccountStateViewModel($"{args.actorName} is not running at the moment");
                }
                catch (Exception e)
                {
                    return new AccountStateViewModel($"{args.actorName} {e.Message}");
                }
            });

            Get("/{actorName}/info", async args =>
            {
                try
                {
                    string account = args.actorName;
                    var path = $@"/user/demoSupervisor/*/{account}";
                    var system = LoanerActors.DemoActorSystem
                        .ActorSelection(path)
                        .ResolveOne(TimeSpan.FromSeconds(3)).Result;


                    if (system.IsNobody()) throw new ActorNotFoundException();
                    var response = await Task.Run(
                        () => system.Ask<MyAccountStatus>(new TellMeYourInfo(), TimeSpan.FromSeconds(3))
                    );
                    response.AccountState.AuditLog.Sort((x, y) => x.EventDate >= y.EventDate ? 1 : -1);
                    return Response.AsJson(new AccountStateViewModel(response.AccountState));
                }
                catch (ActorNotFoundException)
                {
                    return new AccountStateViewModel($"{args.actorName} is not running at the moment");
                }
                catch (Exception e)
                {
                    return new AccountStateViewModel($"{args.actorName} {e.Message}");
                }
            });
     
            Post("/{actorName}/pay", async args =>
            {
                string account = args.actorName;
                var payment = this.Bind<Payment>();

                try
                {
                    var domanCommand = new PayAccount(account, payment.AmountToPay);
                    var system = LoanerActors.DemoActorSystem
                        .ActorSelection($"/user/demoSupervisor/*/{account}")
                        .ResolveOne(TimeSpan.FromSeconds(3));
                    if (system.Exception != null) throw system.Exception;
                    var response = await Task.Run(
                        () => system.Result.Ask<MyAccountStatus>(domanCommand, TimeSpan.FromSeconds(30))
                    );
                    return Response.AsJson(response);
                }
                catch (ActorNotFoundException)
                {
                    return new AccountStateViewModel($"{args.actorName} is not running at the moment");
                }
                catch (Exception e)
                {
                    return new AccountStateViewModel($"{args.actorName} {e.Message}");
                }
            });
//            
//            Get("/{actorName}/assessment", args =>
//            {
//                InvoiceLineItem[] lineItems =
//                {
//                    new InvoiceLineItem(new Tax(0)),
//                    new InvoiceLineItem(new Dues(0)),
//                    new InvoiceLineItem(new Reserve(0))
//                };
//
//
//                return lineItems;
//            });
//            Post("/{actorName}/assessment", async args =>
//            {
//                string account = args.actorName;
//                var assessment = this.Bind<SimulateAssessmentModel>();
//
//                try
//                {
//                    var domanCommand = new BillingAssessment(account, assessment.LineItems);
//                    var system = LoanerActors.DemoActorSystem
//                        .ActorSelection($"/user/demoSupervisor/*/{account}")
//                        .ResolveOne(TimeSpan.FromSeconds(3));
//                    if (system.Exception != null) throw system.Exception;
//                    var response = await Task.Run(
//                        () => system.Result.Ask<MyAccountStatus>(domanCommand, TimeSpan.FromSeconds(30))
//                    );
//                    return Response.AsJson(response);
//                }
//                catch (ActorNotFoundException)
//                {
//                    return new AccountStateViewModel($"{args.actorName} is not running at the moment");
//                }
//                catch (Exception e)
//                {
//                    return new AccountStateViewModel($"{args.actorName} {e.Message}");
//                }
//            });
        }
    }
}
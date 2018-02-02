namespace Lighthouse.NetCoreApp.API.Controllers
{
    using System;
    using static ActorManagement.Management;
    using Nancy;
   
     public class HomeModule : NancyModule
    {
        public HomeModule() : base("/")
        {
            Get("/",  args =>
            {
                return "Hollo, I am Lighthouse, and it's " + DateTime.Now;
            });
            Get("/stop", args =>
            {
                LighthouseActorService.StopAsync().Wait();
                return "Stopping Lighthouse at " + DateTime.Now;
            });
            Get("/restart", args =>
            {
                LighthouseActorService.StopAsync().Wait();

                LighthouseActorService = new LighthouseService();

                LighthouseActorService.Start();

                return "Restarting Lighthouse at " + DateTime.Now;
            });
        }

    }
}

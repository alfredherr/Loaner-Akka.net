
namespace Lighthouse.NetCoreApp.API.Controllers
{
    using System;
    using static ActorManagement.Management;
    using Nancy;
    using System.Linq;
    using Nancy.Routing;
    using Models;

   
     public class HomeModule : NancyModule
    {
        // add dependency to IRouteCacheProvider
        public HomeModule(IRouteCacheProvider rc) : base("/")
        {
            routeCache = rc;
            Get("/", p => View["index", GetIndex()],null,"Shows available routes.");
            
            
            //Stop Lighthouse
            Get("/stop", args =>
            {
                LighthouseActorService.StopAsync().Wait();
                return "Stopping Lighthouse at " + DateTime.Now;
            },null,"Stops Lighthouse.");
            
            //Restart Lighthouse
            Get("/restart", args =>
            {
                LighthouseActorService.StopAsync().Wait();

                LighthouseActorService = new LighthouseService();

                LighthouseActorService.Start();

                return "Restarting Lighthouse at " + DateTime.Now;
            },null,"Restarts Lighthouse.");
        }
        
        private IRouteCacheProvider routeCache;

      
        
        private HomeModel GetIndex()
        {
            var response = new HomeModel();

            // get the cached routes
            IRouteCache cache = routeCache.GetCache();

            response.Routes = cache.Values.SelectMany(t => t.Select(t1 => t1.Item2));

            return response;
        }
    }
}

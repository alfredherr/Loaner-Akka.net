using System.Linq;
using Loaner.API.Models;
using Nancy;
using Nancy.Routing;

namespace Loaner.API.Controllers
{
    public class HomeModule : NancyModule
    {
        private readonly IRouteCacheProvider routeCache;

        // add dependency to IRouteCacheProvider
        public HomeModule(IRouteCacheProvider rc)
        {
            routeCache = rc;
            Get("/", p => View["index", GetIndex()]);
        }

        private HomeModel GetIndex()
        {
            var response = new HomeModel();

            // get the cached routes
            var cache = routeCache.GetCache();

            response.Routes = cache.Values.SelectMany(t => t.Select(t1 => t1.Item2));

            return response;
        }
    }
}
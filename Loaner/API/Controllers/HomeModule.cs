using System.Collections.Generic;
using System.Linq;
using Nancy.Routing;

namespace Loaner.api.Controllers
{
    using Nancy;

    public class HomeModule : NancyModule
    {
        // add dependency to IRouteCacheProvider
        public HomeModule(Nancy.Routing.IRouteCacheProvider rc)
        {
            routeCache = rc;
            Get("/", p => View["index", GetIndex()]);
        }

        private Nancy.Routing.IRouteCacheProvider routeCache;

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

public class HomeModel
{
    public IEnumerable<Nancy.Routing.RouteDescription> Routes { get; set; }
}
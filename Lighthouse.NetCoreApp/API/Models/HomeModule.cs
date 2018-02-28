using System.Collections.Generic;
using Nancy.Routing;

namespace Lighthouse.NetCoreApp.API.Models
{
    public class HomeModel
    {
        public IEnumerable<RouteDescription> Routes { get; set; }
    }
}
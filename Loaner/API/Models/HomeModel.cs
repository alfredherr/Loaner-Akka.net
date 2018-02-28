using System.Collections.Generic;
using Nancy.Routing;

namespace Loaner.API.Models
{
    public class HomeModel
    {
        public IEnumerable<RouteDescription> Routes { get; set; }
    }
}
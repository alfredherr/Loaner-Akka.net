namespace Lighthouse.NetCoreApp.API.Models
{
    using System.Collections.Generic;
    using Nancy.Routing;
    
    public class HomeModel
    {
        public IEnumerable<RouteDescription> Routes { get; set; }
    }

}
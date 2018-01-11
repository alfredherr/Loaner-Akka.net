using System.Collections.Generic;

namespace Loaner.API.Models
{
    public class HomeModel
    {
        public IEnumerable<Nancy.Routing.RouteDescription> Routes { get; set; }
    }
}
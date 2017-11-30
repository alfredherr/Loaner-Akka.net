
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates.StateModels;

namespace Loaner.api.Controllers
{
    using Nancy;
    using System;
    using Nancy.ModelBinding;
    
    public class HomeModule : NancyModule
    {
        public HomeModule() : base("/")
        {
            Get("/", args => {
                return "Hello";
            });
        }
        
    }
}
using System;
using Loaner.BoundedContexts.MaintenanceBilling.DomainModels.Serizalizers.Json;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Diagnostics;
using Nancy.Json;
using Nancy.ModelBinding;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using static Nancy.Json.Json;

namespace Loaner.Configuration
{
    public class DemoBootstrapper : DefaultNancyBootstrapper
    {
        private readonly IAppConfiguration _appConfig;

        public DemoBootstrapper()
        {
        }

        public DemoBootstrapper(IAppConfiguration appConfig)
        {
            _appConfig = appConfig;

        }
        
        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            container.Register(_appConfig);
            //container.Register<JavaScriptConverter,CustomFinancialBucketConverter>();
            //container.Register<JavaScriptSerializer, CustomJavaScriptSerializer>();
            //Console.WriteLine($"I'm running in  ConfigureApplicationContainer() ");

            
        }

        public override void Configure(INancyEnvironment environment)
        {
            environment.Diagnostics(true, "AkkaPassword");
            base.Configure(environment);
            environment.Tracing(enabled: true, displayErrorTraces: true);
        }
    }
}
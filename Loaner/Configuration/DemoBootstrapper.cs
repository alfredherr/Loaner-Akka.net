
using Nancy;
using Nancy.Configuration;
using Nancy.Diagnostics;
using Nancy.TinyIoc;

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

        }

        public override void Configure(INancyEnvironment environment)
        {
            environment.Diagnostics(true, "AkkaPassword");
            base.Configure(environment);
            environment.Tracing(enabled: true, displayErrorTraces: true);
        }
    }
}
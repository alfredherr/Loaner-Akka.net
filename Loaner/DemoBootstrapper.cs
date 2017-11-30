namespace Loaner
{
    using Nancy.Configuration;
    using Nancy;
    using Nancy.TinyIoc;

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
            base.Configure(environment);
            environment.Tracing(enabled: false, displayErrorTraces: true);
        }
    }
}
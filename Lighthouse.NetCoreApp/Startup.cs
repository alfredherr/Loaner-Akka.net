
namespace Lighthouse.NetCoreApp
{

    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Nancy.Owin;
    using NLog.Extensions.Logging;
    using NLog.Web;
    using static ActorManagement.Management;
     
    public class Startup
    {
      
        private readonly IConfiguration _config;

        public Startup(IHostingEnvironment env)
        {
            env.ConfigureNLog("nlog.config");

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath);

            StartService();

            _config = builder.Build();
        }
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseOwin(x => x.UseNancy());

            //add NLog to ASP.NET Core
            loggerFactory.AddNLog();

            //add NLog.Web
            app.AddNLogWeb();
        }

        public void StartService(){
            LighthouseActorService = new LighthouseService();
            LighthouseActorService.Start();
        }
    }
}

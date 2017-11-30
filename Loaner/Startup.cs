namespace Loaner
{
    using BoundedContexts.MaintenanceBilling.Aggregates.Messages;
    using System;
    using static System.Int32;
    using static ActorManagement.LoanerActors;
    using Akka.Actor;
    using Akka.Configuration;
    using Akka.Monitoring;
    using Akka.Monitoring.StatsD;
    using BoundedContexts.MaintenanceBilling.Aggregates;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Nancy.Owin;
    using NLog.Extensions.Logging;
    using NLog.Web;

    public class Startup
    {
        private readonly IConfiguration _config;
        public Startup(IHostingEnvironment env)
        {
            env.ConfigureNLog("nlog.config");
            DemoActorSystem = ActorSystem.Create("demoSystem", GetConfiguration());

            DemoSystemSupervisor = DemoActorSystem.ActorOf(Props.Create<SystemSupervisor>(), "demoSupervisor");

            Console.WriteLine($"StatsDServer: {GetConfigValue("StatsDServer")}");
            Console.WriteLine($"StatsDPort:   {GetConfiValueInt("StatsDPort")}");
            Console.WriteLine($"StatsDPrefix: {GetConfigValue("StatsDPrefix")}");



            ActorMonitoringExtension.RegisterMonitor(DemoActorSystem,
                   new ActorStatsDMonitor(host: GetConfigValue("StatsDServer")
                                        , port: GetConfiValueInt("StatsDPort")
                                        , prefix: GetConfigValue("StatsDPrefix")
                    ));

            DemoSystemSupervisor.Tell(new AboutMe("Starting Up"));

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .SetBasePath(env.ContentRootPath);

            _config = builder.Build();
        }


        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            var appConfig = new AppConfiguration();
            ConfigurationBinder.Bind(_config, appConfig);

            app.UseOwin(x => x.UseNancy(opt => opt.Bootstrapper = new DemoBootstrapper(appConfig)));

            //add NLog to ASP.NET Core
            loggerFactory.AddNLog();

            //add NLog.Web
            app.AddNLogWeb();

        }


        private int GetConfiValueInt(string key)
        {
            TryParse(Environment.GetEnvironmentVariable(key), out int port);
            return port != 0 ? port : 8125;

        }

        private string GetConfigValue(string key)
        {
            string result;
            try
            {
                result = Environment.GetEnvironmentVariable(key);
                if (string.IsNullOrEmpty(result))
                {
                    result = "localhost";
                }
            }
            catch
            {
                result = "localhost";
            }

            return result;

        }
        private static Config GetConfiguration()
        {
            var hocon = @" 

               akka 
               {
                    actor 
                    {
                     serializers 
                        {
                          hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion"" 
                        }
                        serialization-bindings 
                        {
                            ""System.Object"" = hyperion
                        }
                    }
                }
                #akka.suppress-json-serializer-warning = on
                
                akka.actor.debug.lifecycle = on
                akka.actor.debug.unhandled = on
                

                akka.loglevel = DEBUG
                
                akka.loggers=[""Akka.Logger.NLog.NLogLogger, Akka.Logger.NLog""]
                     
            
                ## SqLite
                akka.persistence.journal.plugin = ""akka.persistence.journal.sqlite""
                akka.persistence.journal.sqlite.class = ""Akka.Persistence.Sqlite.Journal.SqliteJournal, Akka.Persistence.Sqlite""
                akka.persistence.journal.sqlite.plugin-dispatcher = ""akka.actor.default-dispatcher""
                akka.persistence.journal.sqlite.connection-timeout = 30s
                akka.persistence.journal.sqlite.table-name = event_journal
                akka.persistence.journal.sqlite.metadata-table-name = journal_metadata
                akka.persistence.journal.sqlite.auto-initialize = on
                akka.persistence.journal.sqlite.timestamp-provider = ""Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common""
                akka.persistence.journal.sqlite.connection-string = ""Data Source=../../../akka_demo.db""
                #
                #akka.persistence.snapshot-store.plugin = ""akka.persistence.snapshot-store.sqlite""
                #akka.persistence.snapshot-store.sqlite.connection-string = ""Data Source=../../../akka_demo.db""
                #akka.persistence.snapshot-store.sqlite.class = ""Akka.Persistence.Sqlite.Snapshot.SqliteSnapshotStore, Akka.Persistence.Sqlite""
                #akka.persistence.snapshot-store.sqlite.plugin-dispatcher = ""akka.actor.default-dispatcher""
                #akka.persistence.snapshot-store.sqlite.connection-timeout = 30s
                #akka.persistence.snapshot-store.sqlite.table-name = snapshot_store
                #akka.persistence.snapshot-store.sqlite.auto-initialize = on

                akka.actor.provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                akka.remote.log-remote-lifecycle-events = DEBUG
                akka.remote.dot-netty.tcp.hostname = ""127.0.0.1""
                akka.remote.dot-netty.tcp.port = 0
                akka.cluster.seed-nodes = [""akka.tcp://demoSystem@127.0.0.1:4053""] 
                akka.cluster.roles = [concord]

           ";
            return ConfigurationFactory.ParseString(hocon);

        }
    }
}

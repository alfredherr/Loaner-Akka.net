namespace Loaner
{
    using BoundedContexts.MaintenanceBilling.Aggregates.Messages;
    using System;
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
    using System.Collections.Generic;
    using System.Text;
    using Akka.Routing;
    using Confluent.Kafka;
    using Confluent.Kafka.Serialization;
    using Configuration;
    using KafkaProducer;

    public class Startup
    {
        private readonly IConfiguration _config;
        public Startup(IHostingEnvironment env)
        {
            env.ConfigureNLog("nlog.config");

            var config = GetConfiguration();

            ConfigureActorSystem(config);

            ConfigureKafkaProducerActors(config);

            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .SetBasePath(env.ContentRootPath);

            _config = builder.Build();
        }

        private void ConfigureActorSystem(Config config)
        {
            DemoActorSystem = ActorSystem.Create("demoSystem", config);

            DemoSystemSupervisor = DemoActorSystem.ActorOf(Props.Create<SystemSupervisor>(), "demoSupervisor");

            var statsDServer = config.GetString("Akka.StatsDServer");
            int statsDPort = Convert.ToInt32(config.GetString("Akka.StatsDPort"));
            var statsDPrefix = config.GetString("Akka.StatsDPrefix");
            BusinessRulesFilename = config.GetString("Akka.BusinessRulesFilename");
            CommandsToRulesFilename = config.GetString("Akka.CommandsToRulesFilename");

            Console.WriteLine($"(StatsD) Server: {statsDServer}");
            Console.WriteLine($"(StatsD) Port:   {statsDPort}");
            Console.WriteLine($"(StatsD) Prefix: {statsDPrefix}");

            Console.WriteLine($"(Business Rules) BusinessRulesFilename: {BusinessRulesFilename}");
            Console.WriteLine($"(Business Rules) CommandsToRulesFilename: {CommandsToRulesFilename}");

            ActorMonitoringExtension.RegisterMonitor(DemoActorSystem,
                new ActorStatsDMonitor(host: statsDServer
                    , port: statsDPort
                    , prefix: statsDPrefix
                ));

            DemoSystemSupervisor.Tell(new TellMeAboutYou("Starting Up"));


        }
        
        private void ConfigureKafkaProducerActors( Config config)
        {
            
            var numAccountPublishers = Convert.ToInt32(config.GetString("Akka.NumAccountPublisherActor"));
            //var numPortfolioPublishers = Convert.ToInt32(config.GetString("Akka.NumPortfolioPublisherActor"));
            var brokerList = config.GetString("Kafka.BrokerList");
            AccountStateKafkaTopicName = config.GetString("Akka.AccountStateTopicName");

            Console.WriteLine($"(kafka) Number of Account Publishers:   {numAccountPublishers}");
            //Console.WriteLine($"(kafka) Number of Portfolio Publishers: {numPortfolioPublishers}");
            Console.WriteLine($"(kafka) List of brokers: {brokerList}");

           // Create the Kafka Producer object for use by the actual actors
            var kafkaConfig = new Dictionary<string, object>()
            {
                ["bootstrap.servers"] = brokerList,
                //["retries"] = 20,
                //["retry.backoff.ms"] = 1000,
                ["client.id"] = "akks-arch-demo",
                //["socket.nagle.disable"] = true,
                ["default.topic.config"] = new Dictionary<string, object>()
                {
                    ["acks"] = -1,
                    ["message.timeout.ms"] = 60000,
                }
            };

            // Create the Kafka Producer
            MyKafkaProducer = new Producer<string, string>(kafkaConfig, new StringSerializer(Encoding.UTF8), new StringSerializer(Encoding.UTF8));

            // Subscribe to error, log and statistics
            MyKafkaProducer.OnError += (obj, error) =>
            {
                Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Error: {error} Obj: {obj}");
                Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Obj Type: {obj.GetType()}");

                if (obj.GetType() == MyKafkaProducer.GetType())
                {
                    Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Type matches produucer");
                    Producer<string, string> temp = (Producer<string, string>)obj;
                }
            };

            MyKafkaProducer.OnLog += (obj, error) =>
            {
                Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Log: {error}");
            };

            MyKafkaProducer.OnStatistics += (obj, error) =>
            {
                Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Statistics: {error}");
            };

            // Schedule the flush actor so we flush the producer on a regular basis
            Props publisherFlushProps = Props.Create(() => new KafkaPublisherFlushActor(MyKafkaProducer));

            AccountStateFlushActor = DemoActorSystem.ActorOf(publisherFlushProps, "publisherFlushActor");

            DemoActorSystem.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(5), AccountStateFlushActor, new Flush(), ActorRefs.NoSender);
            
            // Create the publisher actors for the AccountState
            var actorName = "AccountStatePublisherActor";

            Props accountStatePublisherProps = Props.Create(() => new KafkaPublisherActor(AccountStateKafkaTopicName, MyKafkaProducer, actorName))
                .WithRouter(new RoundRobinPool(numAccountPublishers));

            AccountStatePublisherActor = DemoActorSystem.ActorOf(accountStatePublisherProps, actorName);
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
                 
                akka.actor.debug.lifecycle = on
                akka.actor.debug.unhandled = on
                

                akka.loglevel = DEBUG
                
                akka.loggers=[""Akka.Logger.NLog.NLogLogger, Akka.Logger.NLog""]
                     
                ### PostgreSQL Journal ###
                #akka.persistence.journal.plugin = ""akka.persistence.journal.postgresql""
                #akka.persistence.journal.postgresql.class = ""Akka.Persistence.PostgreSql.Journal.PostgreSqlJournal, Akka.Persistence.PostgreSql""
                #akka.persistence.journal.postgresql.plugin-dispatcher = ""akka.actor.default-dispatcher""
                #akka.persistence.journal.postgresql.connection-string = ""Server=127.0.0.1;Port=5432;Database=akka;User Id=akka;Password=Testing123;""
                #akka.persistence.journal.postgresql.connection-timeout = 30s
                #akka.persistence.journal.postgresql.schema-name = public
                #akka.persistence.journal.postgresql.table-name = event_journal  
                #akka.persistence.journal.postgresql.auto-initialize = on
                #akka.persistence.journal.postgresql.timestamp-provider = ""Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common""
                #akka.persistence.journal.postgresql.metadata-table-name = metadata
                ## defines column db type used to store payload. Available option: BYTEA (default), JSON, JSONB
                #akka.persistence.journal.postgresql.stored-as = BYTEA
                
                ### SqLite Journal ###
                #akka.persistence.journal.plugin = ""akka.persistence.journal.sqlite""
                #akka.persistence.journal.sqlite.class = ""Akka.Persistence.Sqlite.Journal.SqliteJournal, Akka.Persistence.Sqlite""
                #akka.persistence.journal.sqlite.plugin-dispatcher = ""akka.actor.default-dispatcher""
                #akka.persistence.journal.sqlite.connection-timeout = 30s
                #akka.persistence.journal.sqlite.table-name = event_journal
                #akka.persistence.journal.sqlite.metadata-table-name = journal_metadata
                #akka.persistence.journal.sqlite.auto-initialize = on
                #akka.persistence.journal.sqlite.timestamp-provider = ""Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common""
                #akka.persistence.journal.sqlite.connection-string = ""Data Source=../../../akka_demo.db""
                
                ### SqLite Snapshot ###
                #akka.persistence.snapshot-store.plugin = ""akka.persistence.snapshot-store.sqlite""
                #akka.persistence.snapshot-store.sqlite.class = ""Akka.Persistence.Sqlite.Snapshot.SqliteSnapshotStore, Akka.Persistence.Sqlite""
                #akka.persistence.snapshot-store.sqlite.plugin-dispatcher = ""akka.actor.default-dispatcher""
                #akka.persistence.snapshot-store.sqlite.connection-timeout = 30s
                #akka.persistence.snapshot-store.sqlite.table-name = snapshot_store
                #akka.persistence.snapshot-store.sqlite.auto-initialize = on
                #akka.persistence.snapshot-store.sqlite.connection-string = ""Data Source=../../../akka_demo.db""

                ### Jonfile Snapshot ###
		        akka.persistence.snapshot-store.jonfile.class = ""SnapShotStore.FileSnapshotStore3, SnapShotStore""
                akka.persistence.snapshot-store.jonfile.max-load-attempts=19
                akka.persistence.snapshot-store.jonfile.dir = ""C:\\temp""
                akka.persistence.snapshot-store.jonfile.plugin-dispatcher = ""akka.actor.default-dispatcher""
                akka.persistence.snapshot-store.plugin = ""akka.persistence.snapshot-store.jonfile""

                ### Kafka Config ####
                Akka.NumAccountPublisherActor = 10 
                Akka.NumPortfolioPublisherActor = 10 
                Akka.AccountStateTopicName = ""jontest103"" 
                Kafka.BrokerList = ""docker01.concordservicing.com:29092,docker02.concordservicing.com:29092,docker03.concordservicing.com:29092""

                ### StatsD Config ##
                Akka.StatsDServer = ""docker04""
                Akka.StatsDPort   = 8125  
                Akka.StatsDPrefix = ""akka-demo"" 

                ### Business Rules Config ###
                Akka.BusinessRulesFilename = ""C:\\Temp\\business_rules_map.rules""
                Akka.CommandsToRulesFilename = ""C:\\Temp\\commands_to_rules.rules""


                ### Akka Clustering Config ###
                akka.actor.provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                akka.remote.log-remote-lifecycle-events = INFO
                akka.remote.dot-netty.tcp.hostname = ""127.0.0.1""
                akka.remote.dot-netty.tcp.port = 0
                akka.cluster.seed-nodes = [""akka.tcp://demoSystem@127.0.0.1:4053""] 
                akka.cluster.roles = [concord]


           ";
             var conf = ConfigurationFactory.ParseString(hocon);
            return conf;
        }
    }
}

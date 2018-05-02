using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Akka.Actor;
using Akka.Configuration;
using Akka.Monitoring;
using Akka.Monitoring.StatsD;
using Akka.Routing;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Loaner.ActorManagement;
using Loaner.BoundedContexts.MaintenanceBilling.Aggregates;
using Loaner.BoundedContexts.MaintenanceBilling.BusinessRules.Handler;
using Loaner.Configuration;
using Loaner.KafkaProducer;
using Loaner.KafkaProducer.Commands;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nancy.Owin;
using NLog.Extensions.Logging;
using NLog.Web;
using Microsoft.Extensions.DependencyInjection;
using StackifyMiddleware;

namespace Loaner
{
    using static LoanerActors;

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
            var statsDPort = Convert.ToInt32(config.GetString("Akka.StatsDPort"));
            var statsDPrefix = config.GetString("Akka.StatsDPrefix");
            BusinessRulesFilename = config.GetString("Akka.BusinessRulesFilename");
            CommandsToRulesFilename = config.GetString("Akka.CommandsToRulesFilename");

            Console.WriteLine($"(StatsD) Server: {statsDServer}");
            Console.WriteLine($"(StatsD) Port:   {statsDPort}");
            Console.WriteLine($"(StatsD) Prefix: {statsDPrefix}");


            ActorMonitoringExtension.RegisterMonitor(DemoActorSystem,
                new ActorStatsDMonitor(statsDServer
                    , statsDPort
                    , statsDPrefix
                ));

            DemoSystemSupervisor.Tell(new BootUp("Starting Up"));

            Console.WriteLine($"(Business Rules) BusinessRulesFilename: {BusinessRulesFilename}");
            Console.WriteLine($"(Business Rules) CommandsToRulesFilename: {CommandsToRulesFilename}");


            AccountBusinessRulesMapperRouter = DemoActorSystem.ActorOf(Props.Create<AccountBusinessRulesMapper>(), "AccountBusinessRulesMapperRouter");
            AccountBusinessRulesMapperRouter.Tell(new BootUp("Get up!"));
            Console.WriteLine($"(Business Rules) AccountBusinessRulesMapperRouter spun up");
            
            AccountBusinessRulesHandlerRouter = DemoActorSystem.ActorOf(Props.Create<AccountBusinessRulesHandler>(),"AccountBusinessRulesHandlerRouter");
            AccountBusinessRulesHandlerRouter.Tell(new BootUp("Get up!"));
            Console.WriteLine($"(Business Rules) AccountBusinessRulesHandlerRouter spun up");

        }

        private void ConfigureKafkaProducerActors(Config config)
        {
            var numAccountPublishers = Convert.ToInt32(config.GetString("Akka.NumAccountPublisherActor"));
            var numPortfolioPublishers = Convert.ToInt32(config.GetString("Akka.NumPortfolioPublisherActor"));

            AccountStateKafkaTopicName = config.GetString("Akka.AccountStateTopicName");
            PortfolioStateKafkaTopicName = config.GetString("Akka.PortfolioStateTopicName");

            var brokerList = config.GetString("Kafka.BrokerList");

            Console.WriteLine($"(kafka) Number of Account Publishers:   {numAccountPublishers}");
            Console.WriteLine($"(kafka) Number of Portfolio Publishers: {numPortfolioPublishers}");
            Console.WriteLine($"(kafka) List of brokers: {brokerList}");

            // Create the Kafka Producer object for use by the actual actors
            var kafkaConfig = new Dictionary<string, object>
            {
                ["bootstrap.servers"] = brokerList,
                //["retries"] = 20,
                //["retry.backoff.ms"] = 1000,
                ["client.id"] = "akks-arch-demo",
                //["socket.nagle.disable"] = true,
                ["default.topic.config"] = new Dictionary<string, object>
                {
                    ["acks"] = -1,
                    ["message.timeout.ms"] = 60_000
                }
            };

            // Create the Kafka Producer
            MyKafkaProducer = new Producer<string, string>(kafkaConfig, new StringSerializer(Encoding.UTF8),
                new StringSerializer(Encoding.UTF8));

            // Subscribe to error, log and statistics
            MyKafkaProducer.OnError += (obj, error) =>
            {
                Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Error: {error} Obj: {obj}");
                Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Obj Type: {obj.GetType()}");

                if (obj.GetType() == MyKafkaProducer.GetType())
                {
                    Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Type matches produucer");
                    var temp = (Producer<string, string>) obj;
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
            var publisherFlushProps = Props.Create(() => new KafkaPublisherFlushActor(MyKafkaProducer));

            FlushActor = DemoActorSystem.ActorOf(publisherFlushProps, "publisherFlushActor");

            DemoActorSystem.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(5), FlushActor, new Flush(), ActorRefs.NoSender);

            //Create AccountState publisher
            var accountStatePublisherProps = Props.Create(() =>
                    new KafkaPublisherActor(AccountStateKafkaTopicName, MyKafkaProducer, "AccountStatePublisherActor"))
                .WithRouter(new RoundRobinPool(numAccountPublishers));

            AccountStatePublisherActor =
                DemoActorSystem.ActorOf(accountStatePublisherProps, "AccountStatePublisherActor");

            //Create Porfolio Publisher
            var portfolioStatePublisherProps = Props.Create(() =>
                    new KafkaPublisherActor(PortfolioStateKafkaTopicName, MyKafkaProducer,
                        "PortfolioStatePublisherActor"))
                .WithRouter(new RoundRobinPool(numPortfolioPublishers));

            PortfolioStatePublisherActor =
                DemoActorSystem.ActorOf(portfolioStatePublisherProps, "PortfolioStatePublisherActor");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Add Access-Control-Allow-Origin so that other sites can embedd content from this site
            services.AddLogging();
            services.AddCors();

        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseMiddleware<RequestTracerMiddleware>();
            
            var appConfig = new AppConfiguration();
            _config.Bind(appConfig);

            
            app.UseCors(builder =>
            {
                Console.WriteLine($"[DEBUG]: I allow any CORS.");

                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
            
            app.UseOwin(x => x.UseNancy(opt => opt.Bootstrapper = new DemoBootstrapper(appConfig)));

            //add NLog to ASP.NET Core
            loggerFactory.AddNLog();


        }

        private static Config GetConfiguration()
        {
            var hocon = File.ReadAllText(Directory.GetCurrentDirectory() + "/Configuration/HOCONConfiguration.hocon");
            var conf = ConfigurationFactory.ParseString(hocon);
            return conf;
        }
    }
}
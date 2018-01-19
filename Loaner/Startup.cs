using System.IO;
using Loaner.KafkaProducer.Commands;

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
                    Producer<string, string> temp = (Producer<string, string>) obj;
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

            FlushActor = DemoActorSystem.ActorOf(publisherFlushProps, "publisherFlushActor");

            DemoActorSystem.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(5), FlushActor, new Flush(), ActorRefs.NoSender);

            //Create AccountState publisher
            Props accountStatePublisherProps = Props.Create(() =>
                    new KafkaPublisherActor(AccountStateKafkaTopicName, MyKafkaProducer, "AccountStatePublisherActor"))
                .WithRouter(new RoundRobinPool(numAccountPublishers));

            AccountStatePublisherActor = DemoActorSystem.ActorOf(accountStatePublisherProps, "AccountStatePublisherActor");

            //Create Porfolio Publisher
            Props portfolioStatePublisherProps = Props.Create(() =>
                    new KafkaPublisherActor(PortfolioStateKafkaTopicName, MyKafkaProducer, "PortfolioStatePublisherActor"))
                .WithRouter(new RoundRobinPool(numPortfolioPublishers));

            PortfolioStatePublisherActor = DemoActorSystem.ActorOf(portfolioStatePublisherProps, "PortfolioStatePublisherActor");
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
            var hocon = File.ReadAllText(Directory.GetCurrentDirectory() + "/Configuration/HOCONConfiguration.hocon");
            var conf = ConfigurationFactory.ParseString(hocon);
            return conf;
        }
    }
}
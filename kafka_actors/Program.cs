using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using StatsdClient;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace kafka_actors
{
    public class SimpleClass
    {
        public SimpleClass(string key, int counter, int numMsgs)
        {
            Key = key;
            Counter = counter;
            Msgs = new Dictionary<string, string>(10);
            for (var i = 0; i < numMsgs; i++)
            {
                var textGUID = Guid.NewGuid().ToString();
                Msgs.Add(textGUID, "GUID = " + textGUID);
            }

            var rnd = new Random();
            var size = rnd.Next(1, 50);
            IntArray = new int[size];
            for (var j = 0; j < size; j++) IntArray[j] = rnd.Next();
        }

        public string Key { get; set; }
        public int Counter { get; set; }
        public Dictionary<string, string> Msgs { get; set; }
        public int[] IntArray { get; set; }
    }


    internal class Program
    {
        private static void Main(string[] args)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // Set up the STATSD telemtery
            var statsdConfig = new StatsdConfig
            {
                StatsdServerName = "docker04.concordservicing.com",
                StatsdPort = 8125, // Optional; default is 8125
                Prefix = "akka-kafka-producer" // Optional; by default no prefix will be prepended
            };
            DogStatsd.Configure(statsdConfig);

            // Load the configration 
            var config = ConfigurationFactory.ParseString("log-config-on-start = on \n" +
                                                          "Akka.NumAccountPublisherActor = 10 \n" +
                                                          "Akka.NumPortfolioPublisherActor = 10 \n" +
                                                          //                "Kafka.BrokerList = \"docker07.dest.internal:9092,docker08.dest.inmternal:9092,docker09.dest.internal:9092\" \n" +
                                                          "Kafka.BrokerList = \"docker01.concordservicing.com:29092,docker02.concordservicing.com:29092,docker03.concordservicing.com:29092\" \n" +
                                                          "stdout -loglevel = INFO \n" +
                                                          "loglevel=INFO, " +
                                                          "loggers=[\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"]}");

            // 
            var NumAccountPublishers = Convert.ToInt32(config.GetString("Akka.NumAccountPublisherActor"));
            var NumPortfolioPublishers = Convert.ToInt32(config.GetString("Akka.NumPortfolioPublisherActor"));
            var BrokerList = config.GetString("Kafka.BrokerList");

            var AccountStateTopicName = "jontest103";

            Console.WriteLine(NumAccountPublishers);
            Console.WriteLine(NumPortfolioPublishers);
            Console.WriteLine(BrokerList);

            // Create the container for all the actors
            var actorSystem = ActorSystem.Create("csl-arch-poc", config);

            // Create the Kafka Producer object for use by the actual actors
            var kafkaConfig = new Dictionary<string, object>
            {
                ["bootstrap.servers"] = BrokerList,
//                ["retries"] = 20,
//                ["retry.backoff.ms"] = 1000,
                ["client.id"] = "akks-arch-demo",
//                ["socket.nagle.disable"] = true,
                ["default.topic.config"] = new Dictionary<string, object>
                {
                    ["acks"] = -1,
                    ["message.timeout.ms"] = 60000
                }
            };

            // Create the Kafka Producer
            var producer = new Producer<string, string>(kafkaConfig, new StringSerializer(Encoding.UTF8),
                new StringSerializer(Encoding.UTF8));

            // Subscribe to error, log and statistics
            producer.OnError += (obj, error) =>
            {
                Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Error: {error} Obj: {obj}");
                Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Obj Type: {obj.GetType()}");

                if (obj.GetType().Equals(producer))
                {
                    Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Type matches produucer");
                    var temp = (Producer<string, string>) obj;
                }
            };

            producer.OnLog += (obj, error) =>
            {
                Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Log: {error}");
            };

            producer.OnStatistics += (obj, error) =>
            {
                Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + $"- Statistics: {error}");
            };

            // Schedule the flush actor so we flush the producer on a regular basis
            var publisherFlushProps = Props.Create(() => new KafkaPublisherFlushActor(producer));

            var flushActor = actorSystem.ActorOf(publisherFlushProps, "publisherFlushActor");

            actorSystem.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(5), flushActor, new Flush(), ActorRefs.NoSender);

            // Create the publisher actors for the AccountState
            var actorName = "accountStatePublisherActor";

            var accountStatePublisherProps = Props
                .Create(() => new KafkaPublisherActor(AccountStateTopicName, producer, actorName))
                .WithRouter(new RoundRobinPool(NumAccountPublishers));

            var accountStatePublisherActor = actorSystem.ActorOf(accountStatePublisherProps, actorName);

            // Create the publisher actors for the PortfolioState
            /*
            actorName = "portfolioStatePublisherActor";
            Props portfolioStatePublisherProps = Props.Create(() => new KafkaPublisherActor(PortfolioStateTopicName, producer, actorName))
                .WithRouter(new RoundRobinPool(NumPortfolioPublishers));
            IActorRef portfolioStatePublisherActor = actorSystem.ActorOf(portfolioStatePublisherProps, actorName);
            */

            // Create some AccountState events and send them to kafka
            for (var index = 0; index < 1200000; index++)
            {
                var key = "key" + index;
                var msgToSend = new SimpleClass(key, index, 10);
                accountStatePublisherActor.Tell(new Publish(key, msgToSend));
            }

            Console.WriteLine("Sent topic1");

            // Wait for the messages to be sent
            while (true)
            {
                var ret = producer.Flush(5000);
                Console.WriteLine("Flushing ret=" + ret);
                DogStatsd.Gauge("QueueOfMsgsToSend", ret);

                if (ret == 0)
                {
                    stopWatch.Stop();

                    // Get the elapsed time as a TimeSpan value.
                    var ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value.
                    var elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    Console.WriteLine("RunTime " + elapsedTime);
                }

                Thread.Sleep(5000);
            }
        }
    }
}
using Akka.Actor;
using Akka.Configuration;
using Akka.Monitoring;
using Akka.Monitoring.StatsD;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates;
using Demo.BoundedContexts.MaintenanceBilling.Aggregates.Messages;
using Demo.BoundedContexts.MaintenanceBilling.Commands;
using System;

using static Demo.ActorManagement.LoanerActors;
namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"************************************");
            Console.WriteLine($"************  Startng Demo  ********");
            Console.WriteLine($"************************************");

            DemoActorSystem = ActorSystem.Create("demoSystem", GetConfiguration());

            DemoSystemSupervisor = DemoActorSystem.ActorOf(Props.Create<SystemSupervisor>(), "demoSupervisor");

            ActorMonitoringExtension.RegisterMonitor(DemoActorSystem, new ActorStatsDMonitor(host: "docker04" , port: 8125 , prefix: "akka-demo" ));

            DemoSystemSupervisor.Tell(new AboutMe("Starting Up"));


           
            Console.WriteLine($"************************************");
            Console.WriteLine($"***  [1] = board accounts ******");
            Console.WriteLine($"***  [2] = exit             ******");
            Console.WriteLine($"************************************");
            var input = Console.ReadLine();
            while (!input.Contains("2"))
            {
                Console.WriteLine($"key: {input}.");
                input = Console.ReadLine();
                if (input.Contains("1"))
                {
                    DemoSystemSupervisor.Tell(new SimulateBoardingOfAccounts(
                        "Raintree",
                        "./SampleData/Raintree.txt",
                       "./SampleData/Obligations/Raintree.txt"
                    ));
                    Console.WriteLine($"Sent DemoSystemSupervisor SimulateBoardingOfAccounts message");

                }
            }

            Console.WriteLine($"**********************************");
            Console.WriteLine($"*********  End Of Demo  **********");
            Console.WriteLine($"**********************************");
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
                #akka.persistence.journal.plugin = ""akka.persistence.journal.sqlite""
                #akka.persistence.journal.sqlite.class = ""Akka.Persistence.Sqlite.Journal.SqliteJournal, Akka.Persistence.Sqlite""
                #akka.persistence.journal.sqlite.plugin-dispatcher = ""akka.actor.default-dispatcher""
                #akka.persistence.journal.sqlite.connection-timeout = 30s
                #akka.persistence.journal.sqlite.table-name = event_journal
                #akka.persistence.journal.sqlite.metadata-table-name = journal_metadata
                #akka.persistence.journal.sqlite.auto-initialize = on
                #akka.persistence.journal.sqlite.timestamp-provider = ""Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common""
                #akka.persistence.journal.sqlite.connection-string = ""Data Source=../../../akka_demo.db""
                
                #akka.persistence.snapshot-store.plugin = ""akka.persistence.snapshot-store.sqlite""
                #akka.persistence.snapshot-store.sqlite.class = ""Akka.Persistence.Sqlite.Snapshot.SqliteSnapshotStore, Akka.Persistence.Sqlite""
                #akka.persistence.snapshot-store.sqlite.plugin-dispatcher = ""akka.actor.default-dispatcher""
                #akka.persistence.snapshot-store.sqlite.connection-timeout = 30s
                #akka.persistence.snapshot-store.sqlite.table-name = snapshot_store
                #akka.persistence.snapshot-store.sqlite.auto-initialize = on
                #akka.persistence.snapshot-store.sqlite.connection-string = ""Data Source=../../../akka_demo.db""

                akka.actor.provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
                akka.remote.log-remote-lifecycle-events = INFO
                akka.remote.dot-netty.tcp.hostname = ""127.0.0.1""
                akka.remote.dot-netty.tcp.port = 0
                akka.cluster.seed-nodes = [""akka.tcp://demoSystem@127.0.0.1:4053""] 
                akka.cluster.roles = [concord]

           ";
            return ConfigurationFactory.ParseString(hocon);

        }
    }


}

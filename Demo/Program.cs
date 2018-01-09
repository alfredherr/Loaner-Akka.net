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
                

                akka.loglevel = INFO
                
                akka.loggers=[""Akka.Logger.NLog.NLogLogger, Akka.Logger.NLog""]
                     
            
                ##############################################################
                ## SQLite Journal
                ##############################################################
                akka.persistence.journal.plugin = ""akka.persistence.journal.sqlite""
                akka.persistence.journal.sqlite.class = ""Akka.Persistence.Sqlite.Journal.BatchingSqliteJournal, Akka.Persistence.Sqlite""
                akka.persistence.journal.sqlite.plugin-dispatcher = ""akka.actor.default-dispatcher""
                akka.persistence.journal.sqlite.connection-timeout = 30s
                akka.persistence.journal.sqlite.table-name = event_journal
                akka.persistence.journal.sqlite.metadata-table-name = journal_metadata
                akka.persistence.journal.sqlite.auto-initialize = on
                akka.persistence.journal.sqlite.timestamp-provider = ""Akka.Persistence.Sql.Common.Journal.DefaultTimestampProvider, Akka.Persistence.Sql.Common""
                akka.persistence.journal.sqlite.connection-string = ""Data Source=../../../akka_demo.db""
                akka.persistence.journal.sqlite.max-batch-size = 10000
                #akka.persistence.journal.sqlite.isolation-level = ""read-uncommitted""

                ##############################################################
                ## SQLite Snapshot
                ##############################################################
                #akka.persistence.snapshot-store.plugin = ""akka.persistence.snapshot-store.sqlite""
                #akka.persistence.snapshot-store.sqlite.class = ""Akka.Persistence.Sqlite.Snapshot.SqliteSnapshotStore, Akka.Persistence.Sqlite""
                #akka.persistence.snapshot-store.sqlite.plugin-dispatcher = ""akka.actor.default-dispatcher""
                #akka.persistence.snapshot-store.sqlite.connection-timeout = 30s
                #akka.persistence.snapshot-store.sqlite.table-name = snapshot_store
                #akka.persistence.snapshot-store.sqlite.auto-initialize = on
                #akka.persistence.snapshot-store.sqlite.connection-string = ""Data Source=../../../akka_demo.db""
            
                ##############################################################
                ## RocksDB Journal
                ##############################################################
                #akka.persistence.journal.rocksdb.class = ""Akka.Persistence.RocksDb.Journal.RocksDbJournal, Akka.Persistence.RocksDb""
                #akka.persistence.journal.rocksdb.plugin-dispatcher = ""akka.persistence.dispatchers.default-plugin-dispatcher""
                #akka.persistence.journal.rocksdb.replay-dispatcher = ""akka.persistence.dispatchers.default-replay-dispatcher""
                #akka.persistence.journal.rocksdb.path = ""C:\\dev\\Loaner2\\Demo\\journal""
                #akka.persistence.journal.rocksdb.fsync = on
                #akka.persistence.journal.rocksdb.checksum = off
                
                
                ##############################################################
                ## PostgreSQL Journal
                ##############################################################
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


                ##############################################################
                ## Akka Cluster
                ##############################################################
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

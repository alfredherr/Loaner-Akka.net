using System;
using System.Collections.Generic;
using System.IO;
using Akka.Actor;
using Akka.Configuration;

namespace SnapShotStore
{
    internal class Program
    {
        private const int NUM_SNAPSHOT_ACTORS = 4;

        private static void Main(string[] args)
        {
            var NUM_ACTORS = 0;
            var FILENAME = "";

            try
            {
                NUM_ACTORS = int.Parse(Environment.GetEnvironmentVariable("NUM_ACTORS"));
                Console.WriteLine("ENV NUM_ACTORS={0}", NUM_ACTORS);
                FILENAME = Environment.GetEnvironmentVariable("FILENAME");
                Console.WriteLine("ENV FILENAME={0}", FILENAME);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR trying to obtain value for Env var: ENV NUM_ACTORS & FILENAME. Exception msg={0}", e.Message);
                //return;
            }

            // TODO - Remove these items
            NUM_ACTORS = 60060;
            FILENAME = @"c:\temp\datagen.bin";

            // Get the configuration of the akka system
            var config = ConfigurationFactory.ParseString(GetConfiguration());

            // Create the containers for all the actors. Using multiple systems to see if startup time is reduced
            var actorSystem = ActorSystem.Create("csl-arch-poc1", config);

            // Create the AccountGenertor actor
            var accountGeneratorActorProps = Props.Create(() => new AccountGenerator());
            var agref = actorSystem.ActorOf(accountGeneratorActorProps, "AccountGenerator");

            // Generate the accounts
            agref.Tell(new GenerateAccounts(FILENAME, NUM_ACTORS));

            Console.WriteLine(
                "Press return to send the created account actors a message causing them to save a snapshot");
            Console.ReadLine();

            agref.Tell(new SendMsgs());

            Console.WriteLine("Enter an actor id to probe or E to stop");
            var finished = false;
            var actorPath = "/user/AccountGenerator/testActor-";
            while (finished != true)
            {
                var line = Console.ReadLine();
                if (line.Equals("E"))
                {
                    finished = true;
                }
                else
                {
                    // Get the actor reference and send it a display message
                    Console.WriteLine("Sending a display message to " + actorPath + line);
                    actorSystem.ActorSelection(actorPath + line).Tell(new DisplayState());
                }
            }

            Console.WriteLine("Press return to terminate the system");
            Console.ReadLine();

            // Wait until actor system terminated
            actorSystem.Terminate();
        }


        private static string GetConfiguration()
        {
            return @"
                akka {  
                    stdout-loglevel = DEBUG
                    loglevel = INFO
                    log-config-on-start = on        
#                    loggers = [""Akka.Logger.NLog.NLogLogger, Akka.Logger.NLog""]
                }

                actor
                {
                  debug
                  {
                    receive = on      # log any received message
                    autoreceive = on  # log automatically received messages, e.g. PoisonPill
                    lifecycle = on    # log actor lifecycle changes
                    event-stream = on # log subscription changes for Akka.NET event stream
                    unhandled = on    # log unhandled messages sent to actors
                  }
                }

                # Dispatcher for the Snapshot file store
#                snapshot-dispatcher {
#                  type = Dispatcher
#                  throughput = 10000
#                }

                # Persistence Plugin for SNAPSHOT
                akka.persistence {
#                    journal {
#                        in-mem {
#                            class = ""Akka.Persistence.Journal.MemoryJournal, Akka.Persistence""
                            # Dispatcher for the plugin actor.
#                            plugin - dispatcher = ""akka.actor.default-dispatcher""
#                        }
#                    }

                snapshot-store {
		                jonfile {
			                # qualified type name of the File persistence snapshot actor
            			    class = ""SnapShotStore.FileSnapshotStore3, SnapShotStore""
                            max-load-attempts=19
#                            dir = ""/temp""
                            dir = ""C:\\temp""

                            # dispatcher used to drive snapshot storage actor
                            plugin-dispatcher = ""akka.actor.default-dispatcher""
#                            plugin-dispatcher = ""snapshot-dispatcher""

                        }
                    }
                }

                akka.persistence.snapshot-store.plugin = ""akka.persistence.snapshot-store.jonfile""
#                akka.persistence.journal.plugin = ""akka.persistence.journal.in-mem""

                akka.persistence.max-concurrent-recoveries = 10

                # Dispatcher for the TestActors to see if this changes the performance
 #               test-actor-dispatcher {
 #                   type = ForkJoinDispatcher
 #                   throughput = 10
 #                   dedicated-thread-pool {
 #                       thread-count = 2
 #                       deadlock-timeout = 60s
 #                       threadtype = background
 #                   }
                }

                # Deployment configuration
                akka.actor.deployment {

                    # Configuration for test-actor deployment
  #                  ""/AccountGenerator/*"" {
  #                      dispatcher = test-actor-dispatcher
  #                  }
                }

                # Timeout on recovery of snapshot & journal entries
                journal-plugin-fallback.recovery-event-timeout = 90s
                akka.persistence.snapshot-store.recovery-event-timeout = 60s

            ";
        }


        private static List<Account> CreateAccounts(int limit)
        {
            Console.WriteLine("Creating the accounts");
            var counter = 0;
            string line;
            var list = new List<Account>(limit);

            try
            {
                // Read the file and display it line by line.  
                var file =
                    new StreamReader(@"c:\temp\datagen.bin");
//                new System.IO.StreamReader(@"/temp/datagen.bin");
                while ((line = file.ReadLine()) != null)
                {
                    if (counter == 0)
                    {
                        counter++;
                        continue; // skip the headers in the file
                    }

                    //                System.Console.WriteLine(line);
                    var tokens = line.Split(',');
                    var account = new Account(tokens[0]);

                    account.CompanyIDCustomerID = tokens[1];
                    account.AccountTypeID = tokens[2];
                    account.PrimaryAccountCodeID = tokens[3];
                    account.PortfolioID = int.Parse(tokens[4]);
                    account.ContractDate = tokens[5];
                    account.DelinquencyHistory = tokens[6];
                    account.LastPaymentAmount = tokens[7];
                    account.LastPaymentDate = tokens[8];
                    account.SetupDate = tokens[9];
                    account.CouponNumber = tokens[10];
                    account.AlternateAccountNumber = tokens[11];
                    account.Desc1 = tokens[12];
                    account.Desc2 = tokens[13];
                    account.Desc3 = tokens[14];
                    account.ConversionAccountID = tokens[15];
                    account.SecurityQuestionsAnswered = tokens[16];
                    account.LegalName = tokens[17];
                    account.RandomText0 = Guid.NewGuid() +
                                          "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" +
                                          Guid.NewGuid();
                    account.RandomText1 = Guid.NewGuid() +
                                          "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" +
                                          Guid.NewGuid();
                    account.RandomText3 = Guid.NewGuid() +
                                          "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" +
                                          Guid.NewGuid();
                    account.RandomText4 = Guid.NewGuid() +
                                          "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" +
                                          Guid.NewGuid();
                    account.RandomText5 = Guid.NewGuid() +
                                          "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" +
                                          Guid.NewGuid();
                    account.RandomText6 = Guid.NewGuid() +
                                          "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" +
                                          Guid.NewGuid();
                    account.RandomText7 = Guid.NewGuid() +
                                          "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" +
                                          Guid.NewGuid();
                    account.RandomText8 = Guid.NewGuid() +
                                          "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" +
                                          Guid.NewGuid();
                    account.RandomText9 = Guid.NewGuid() +
                                          "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" +
                                          Guid.NewGuid();

                    // Store the Account in the List
                    list.Add(account);

                    if (counter > limit + 1) break;
                    counter++;
                }

                file.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            Console.WriteLine("Finished creating the accounts");
            return list;
        }
    }
}
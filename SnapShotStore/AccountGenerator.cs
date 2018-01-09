using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;


namespace SnapShotStore
{
    #region Command and Message classes
    public class GenerateAccounts
    {
        public GenerateAccounts(string filename, int numAccountsToGenerate)
        {
            Filename = filename;
            NumAccountsToGenerate = numAccountsToGenerate;        }

        public string Filename { get; private set; }
        public int NumAccountsToGenerate { get; private set; }

    }

    public class DisplayStatistics
    {
    }

    public class PersistState
    {
    }

    public class SendMsgs
    {
    }
    #endregion

    class AccountGenerator : ReceivePersistentActor
    {
        private ILoggingAdapter _log;

        // The actor state to be persisted
        private List<string> AccountList;
        IActorRef[] irefs;


        public override string PersistenceId
        {
            get
            {
                return "AccountGenerator";
            }
        }

        public AccountGenerator()
        {
            _log = Context.GetLogger();

            // Recover
            Recover<SnapshotOffer>(offer => RecoverSnapshot(offer));

            // Commands
            Command<SaveSnapshotSuccess>(cmd => SnapshotSuccess(cmd));
            Command<SaveSnapshotFailure>(cmd => SnapshotFailure(cmd));
            Command<PersistState>(msg => Persist());
            Command<DisplayStatistics>(msg => Display());
            Command<SendMsgs>(msg => Send());
            Command<GenerateAccounts>(msg => Generate(msg));
        }

        protected override void PreStart()
        {
            _log.Debug("PreStart()");

            if (AccountList == null) AccountList = new List<string>(1000000);
        }



        private void SnapshotSuccess(SaveSnapshotSuccess cmd)
        {
            _log.Debug("AccountGenerator - Processing SnapShotSuccess command");
        }

        private void SnapshotFailure(SaveSnapshotFailure cmd)
        {
            _log.Debug("AccountGenerator - Processing SnapShotFailure command");
        }

        private void Persist()
        {
            _log.Debug("AccountGenerator - Processing PersistState command");
            SaveSnapshot(AccountList);
        }

        private void Display()
        {
            _log.Debug("AccountGenerator - Processing DisplayStatistics");
//            Console.WriteLine("PersistenceId={0}, desc={1}", Acc.AccountID, Acc.Desc1);
        }

        private void Send()
        {
            _log.Debug("Send - Begin sending account actors a message to cause them to change their state");

            for (int i = 0; i < irefs.Length; i++)
            {
                irefs[i].Tell(new SomeMessage());
            }

            _log.Debug("Send - End of sending account actor msgs");

        }

        private void RecoverSnapshot(SnapshotOffer offer)
        {
            _log.Debug("AccountGenerator - Processing RecoverSnapshot");
            AccountList = (List<string>)offer.Snapshot;
        }

        private void Generate(GenerateAccounts msg)
        {
            // Create the accounts 
            List<Account> accounts = CreateAccounts(msg);

            // Only create the actors that we need to
            int i;
            irefs = new IActorRef[msg.NumAccountsToGenerate];

            // Start the actors that have previously been created
            for (i=0; i < msg.NumAccountsToGenerate && i < AccountList.Count; i++)
            {
                Props testActorProps = Props.Create(() => new TestActor(AccountList[i]));

                // Spread the actors across the systems to see if we get better performance
                irefs[i] = Context.ActorOf(testActorProps, "testActor-" + i);
            }

            int started = i;
            _log.Info("Generate - started {0} previously created actors", i);

            // Create any new actors that are needed
            for (; i < msg.NumAccountsToGenerate; i++)
            {
                Props testActorProps = Props.Create(() => new TestActor(accounts[i]));
                // Spread the actors across the systems to see if we get better performance
                irefs[i] = Context.ActorOf(testActorProps, "testActor-" + i);

                AccountList.Add(accounts[i].AccountID);
            }

            _log.Info("Generate - created {0} new actors", i - started);

            // Save the state
            SaveSnapshot(AccountList);
        }


        private void CreateActors()
        {
            Console.WriteLine("Hit return to display actor state");
            Console.ReadLine();
/*
            irefs[0].Tell(new DisplayState());
            irefs[1].Tell(new DisplayState());
            irefs[NUM_ACTORS - 2].Tell(new DisplayState());
            irefs[NUM_ACTORS - 1].Tell(new DisplayState());

            // Start the timer to measure how long it takes to complete the test
            Console.WriteLine("Starting the test to persist actor state");
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();


            // Send three msgs to see if the metadata seq number changes
            for (int i = 0; i < NUM_ACTORS; i++)
            {
                irefs[i].Tell(new SomeMessage());
            }

            // Get the elapsed time as a TimeSpan value.
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;

            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime for telling the actors" + elapsedTime);

            Console.WriteLine("Hit return to cause some actors to print out some of their state. This is to check that their state has been saved and restored correctly");
            Console.ReadLine();

            irefs[0].Tell(new DisplayState());
            irefs[1].Tell(new DisplayState());
            irefs[NUM_ACTORS - 2].Tell(new DisplayState());
            irefs[NUM_ACTORS - 1].Tell(new DisplayState());



            Console.WriteLine("Hit return to terminate AKKA");
            Console.ReadLine();

            // Wait until actor system terminated
            actorSystem.Terminate();

            Console.WriteLine("Hit return to terminate program");
            Console.ReadLine();
*/
        }




        private List<Account> CreateAccounts(GenerateAccounts msg)
        {
            _log.Debug("AccountGenerator - Processing GenerateAccounts");

            int counter = 0;
            string line;
            List<Account> list = new List<Account>(msg.NumAccountsToGenerate);

            try
            {

                // Read the file and display it line by line.  
                System.IO.StreamReader file =
                    new System.IO.StreamReader(msg.Filename);

                _log.Debug("AccountGenerator - Start Account Generation");

                while ((line = file.ReadLine()) != null)
                {
                    if (counter == 0)
                    {
                        counter++;
                        continue; // skip the headers in the file
                    }

                    string[] tokens = line.Split(',');
                    Account account = new Account(tokens[0]);

                    account.CompanyIDCustomerID = tokens[1];
                    account.AccountTypeID = tokens[2];
                    account.PrimaryAccountCodeID = tokens[3];
                    account.PortfolioID = Int32.Parse(tokens[4]);
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
                    account.RandomText0 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText1 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText3 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText4 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText5 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText6 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText7 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText8 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();
                    account.RandomText9 = Guid.NewGuid() + "SOme random lot of text that is front and ended with a guid to make it uique and fairly long so it taxes the actor creation mechanism to determine if it takes too long" + Guid.NewGuid();

                    // Store the Account in the List
                    list.Add(account);

                    if (counter > msg.NumAccountsToGenerate + 1) break;
                    counter++;
                }

                _log.Debug("AccountGenerator - Finish Account Generation");

                file.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR opening the faile that holds the data for the accounts. Filename={0}. Error={1}. Stacktrace={2}", msg.Filename, e.Message, e.StackTrace);
            }

            return list;
        }
    }




}

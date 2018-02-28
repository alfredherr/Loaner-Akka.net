using System;
using System.Collections.Generic;
using System.IO;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;

namespace SnapShotStore
{
    #region Command and Message classes

    #endregion

    internal class AccountGenerator : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        // The actor state to be persisted
        private List<string> AccountList;
        private IActorRef[] irefs;

        public AccountGenerator()
        {
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


        public override string PersistenceId => "AccountGenerator";

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

            for (var i = 0; i < irefs.Length; i++) irefs[i].Tell(new SomeMessage());

            _log.Debug("Send - End of sending account actor msgs");
        }

        private void RecoverSnapshot(SnapshotOffer offer)
        {
            _log.Debug("AccountGenerator - Processing RecoverSnapshot");
            AccountList = (List<string>) offer.Snapshot;
        }

        private void Generate(GenerateAccounts msg)
        {
            List<Account> accounts = null;

            if (AccountList.Count >= msg.NumAccountsToGenerate)
                _log.Info("Generate - enough accounts already exist. AccountList.Count={0} NumAccountsToGenerate={1}",
                    AccountList.Count, msg.NumAccountsToGenerate);
            else
                _log.Info("Generate - generating accounts for {0} actors",
                    AccountList.Count - msg.NumAccountsToGenerate);

            // Only create the actors that we need to
            int i;
            irefs = new IActorRef[msg.NumAccountsToGenerate];

            // Start the actors that have previously been created
            for (i = 0; i < msg.NumAccountsToGenerate && i < AccountList.Count; i++)
            {
                var testActorProps = Props.Create(() => new TestActor(AccountList[i]));

                // Spread the actors across the systems to see if we get better performance
                irefs[i] = Context.ActorOf(testActorProps, "testActor-" + i);
            }

            var started = i;
            _log.Info("Generate - started {0} previously created actors", i);

            // Create any new actors that are needed
            if (i < msg.NumAccountsToGenerate) accounts = CreateAccounts(msg);

            for (; i < msg.NumAccountsToGenerate; i++)
            {
                var testActorProps = Props.Create(() => new TestActor(accounts[i]));
                // Spread the actors across the systems to see if we get better performance
                irefs[i] = Context.ActorOf(testActorProps, "testActor-" + i);

                AccountList.Add(accounts[i].AccountID);
            }

            _log.Info("Generate - created {0} new actors", i - started);

            // Save the state
            SaveSnapshot(AccountList);
        }


        private List<Account> CreateAccounts(GenerateAccounts msg)
        {
            _log.Debug("AccountGenerator - Processing GenerateAccounts");

            var counter = 0;
            string line;
            var list = new List<Account>(msg.NumAccountsToGenerate);
            var rnd = new Random();


            try
            {
                // Read the file and display it line by line.  
                var file =
                    new StreamReader(msg.Filename);

                _log.Info("AccountGenerator - Start Account Generation");

                while ((line = file.ReadLine()) != null)
                {
                    if (counter == 0)
                    {
                        counter++;
                        continue; // skip the headers in the file
                    }

                    var tokens = line.Split(',');
                    var account = new Account(tokens[0] + "-" + (counter - 1));

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

                    // Every so often make a large account with a lot of data
                    const string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@$?_-";
                    if (counter % 10000 == 0)
                    {
                        _log.Debug("AccountGenerator - counter={0}", counter);

                        // Populate a large dictionary
                        account.LargeAccount1 = new Dictionary<string, string>();
                        var size = rnd.Next(100000, 200000);
                        for (var i = 0; i < size; i++)
                        {
                            var stringLength = rnd.Next(100, 1000);
                            var chars = new char[stringLength];

                            for (var j = 0; j < stringLength; j++)
                                chars[j] = allowedChars[rnd.Next(0, allowedChars.Length)];

                            account.LargeAccount1.Add(new string(chars) + i, "" + i);
                        }

                        // Populate a large list
                        account.LargeAccount2 = new List<float>();
                        size = rnd.Next(5000, 50000);
                        for (var i = 0; i < size; i++)
                        {
                            var result = rnd.NextDouble() * (float.MaxValue - (double) float.MinValue) + float.MinValue;
                            account.LargeAccount2.Add((float) result);
                        }
                    }

                    // Create an array of random longs
                    account.LongValues = new long[rnd.Next(20, 500)];
                    for (var i = 0; i < account.LongValues.Length; i++) account.LongValues[i] = rnd.Next(1, 18000000);

                    // Store the Account in the List
                    list.Add(account);

                    if (counter > msg.NumAccountsToGenerate + 1)
                        break;
                    counter++;
                }

                _log.Info("AccountGenerator - Finish Account Generation");

                file.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "ERROR opening the faile that holds the data for the accounts. Filename={0}. Error={1}. Stacktrace={2}",
                    msg.Filename, e.Message, e.StackTrace);
            }

            return list;
        }
    }
}
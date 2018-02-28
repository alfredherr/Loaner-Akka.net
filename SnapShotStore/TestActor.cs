using System;
using Akka.Event;
using Akka.Persistence;

namespace SnapShotStore
{
    #region Command and Message classes

    #endregion

    internal class TestActor : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();

        // The actor state to be persisted
        private Account Acc;
        private string AccountId;

        public TestActor(Account acc)
        {
            // Store the actor state 
            Acc = acc;
            AccountId = acc.AccountID;

            Setup();
        }

        public TestActor(string accountId)
        {
            AccountId = accountId;
            Setup();
        }

        public override string PersistenceId => AccountId;

        private void Setup()
        {
//            _log.Info("TestActor - setup - PersistenceId={0}", PersistenceId);
            // Display the configuration for the dispatcher
            // Get the configuration
//            Console.WriteLine("Name={0}", Context.Self.Path);

//            var config = Context.Dispatcher.Configurator.Config;
//            foreach (var item in config.AsEnumerable())
//            {
//                Console.WriteLine("Name={0}, Value={1}", item.Key, item.Value);
//            }

            // Recover
            Recover<SnapshotOffer>(offer => RecoverSnapshot(offer));

            // Commands
            Command<SaveSnapshotSuccess>(cmd => SnapshotSuccess(cmd));
            Command<SaveSnapshotFailure>(cmd => SnapshotFailure(cmd));
            Command<SomeMessage>(msg => Process(msg));
            Command<DisplayState>(msg => Display());
        }


        private void SnapshotSuccess(SaveSnapshotSuccess cmd)
        {
            _log.Debug("Processing SnapShotSuccess command, ID={0}", Acc.AccountID);
        }

        private void SnapshotFailure(SaveSnapshotFailure cmd)
        {
            _log.Error("Processing SnapShotFailure command, ID={0}, cause={1} \nStacktrace={2}", Acc.AccountID,
                cmd.Cause.Message, cmd.Cause.StackTrace);
        }

        private void Process(SomeMessage msg)
        {
            // Modify the actor state 
            Acc.Desc1 = "Hi jon the time is: " + DateTime.Now;
            SaveSnapshot(Acc);
            _log.Debug("Processing SaveSnapshot in testactor, ID={0}", Acc.AccountID);
        }

        private void Display()
        {
            _log.Info("Processing Display in testactor, ID={0}, the new desc is: {1}", Acc.AccountID, Acc.Desc1);
            Console.WriteLine("PersistenceId={0}, desc={1}", Acc.AccountID, Acc.Desc1);
        }

        private void RecoverSnapshot(SnapshotOffer offer)
        {
            Acc = (Account) offer.Snapshot;
            if (Acc == null)
            {
                _log.Error("ERROR in RecoverSnapshot. PersistenceId = {0}", AccountId);
            }
            else
            {
                AccountId = Acc.AccountID;
                _log.Debug("Finished Processing RecoverSnapshot, ID={0}", Acc.AccountID);
            }
        }
    }
}
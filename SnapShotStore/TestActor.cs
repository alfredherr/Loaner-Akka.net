using System;
using Akka.Event;
using Akka.Persistence;
using SnapShotStore.Messages;

namespace SnapShotStore
{
    #region Command and Message classes

    #endregion

    internal class TestActor : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _log;

        // The actor state to be persisted
        private Account Acc;
        private string AccountId;

        public TestActor(Account acc)
        {
            _log = Context.GetLogger();

            // Store the actor state 
            Acc = acc;
            AccountId = acc.AccountID;

            Setup();
        }

        public TestActor(string accountId)
        {
            _log = Context.GetLogger();

            AccountId = accountId;
            Setup();
        }

        public override string PersistenceId => AccountId;

        private void Setup()
        {
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
            _log.Debug("Processing SnapShotFailure command, ID={0}, cause={1} \nStacktrace={2}", Acc.AccountID,
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
            _log.Debug("Processing CompareState in testactor, ID={0}, the new desc is: {1}", Acc.AccountID, Acc.Desc1);
            Console.WriteLine("PersistenceId={0}, desc={1}", Acc.AccountID, Acc.Desc1);
        }

        private void RecoverSnapshot(SnapshotOffer offer)
        {
            Acc = (Account) offer.Snapshot;
            AccountId = Acc.AccountID;
            _log.Debug("Finished Processing RecoverSnapshot, ID={0}", Acc.AccountID);
        }
    }
}
using Akka.Routing;

namespace SnapShotStore
{
    public class SaveSnapshot : IConsistentHashable
    {
        public SaveSnapshot(string id, object state)
        {
            ID = id;
            State = state;
        }

        public object State { get; }
        public string ID { get; }
        public object ConsistentHashKey => ID;
    }
}
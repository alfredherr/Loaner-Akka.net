using Akka.Routing;

namespace SnapShotStore
{
    public class SaveSnapshot : IConsistentHashable
    {
        public SaveSnapshot(string id, object state)
        {
            this.ID = id;
            this.State = state;
        }

        public object State { get; private set; }
        public string ID { get; private set; }
        public object ConsistentHashKey { get { return ID; } }
    }
}
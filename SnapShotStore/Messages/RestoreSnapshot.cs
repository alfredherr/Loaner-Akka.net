namespace SnapShotStore.Messages
{
    public class RestoreSnapshot
    {
        public RestoreSnapshot(string id)
        {
            ID = id;
        }

        public string ID { get; }
    }
}
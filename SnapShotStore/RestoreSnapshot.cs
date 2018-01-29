namespace SnapShotStore
{
    public class RestoreSnapshot
    {
        public RestoreSnapshot(string id)
        {
            this.ID = id;
        }

        public string ID { get; private set; }
    }
}
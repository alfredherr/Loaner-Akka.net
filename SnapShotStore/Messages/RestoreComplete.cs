namespace SnapShotStore.Messages
{
    public class RestoreComplete
    {
        public RestoreComplete(object obj)
        {
            Obj = obj;
        }

        public object Obj { get; }
    }
}
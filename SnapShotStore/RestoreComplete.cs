namespace SnapShotStore
{
    public class RestoreComplete
    {
        public RestoreComplete(object obj)
        {
            this.Obj = obj;
        }

        public object Obj { get; private set; }
    }
}
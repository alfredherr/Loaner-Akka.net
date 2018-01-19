namespace Loaner.KafkaProducer.Commands
{
    public class Publish
    {
        public Publish(string key, object msg)
        {
            this.Key = key;
            this.Msg = msg;
        }

        public string Key { get; private set; }
        public object Msg { get; private set; }
    }
}
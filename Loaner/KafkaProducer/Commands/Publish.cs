namespace Loaner.KafkaProducer.Commands
{
    public class Publish
    {
        public Publish(string key, object msg)
        {
            Key = key;
            Msg = msg;
        }

        public string Key { get; }
        public object Msg { get; }
    }
}
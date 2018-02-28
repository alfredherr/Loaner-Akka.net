namespace Loaner.KafkaProducer.Commands
{
    public class Resend
    {
        public Resend(string key, string msg)
        {
            Key = key;
            Msg = msg;
        }

        public string Key { get; }
        public string Msg { get; }
    }
}
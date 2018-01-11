namespace Loaner.KafkaProducer
{
    public class Resend
    {
        public Resend(string key, string msg)
        {
            this.Key = key;
            this.Msg = msg;
        }

        public string Key { get; private set; }
        public string Msg { get; private set; }
    }
}
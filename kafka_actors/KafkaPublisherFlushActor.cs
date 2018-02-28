using Akka.Actor;
using Confluent.Kafka;
using StatsdClient;

namespace kafka_actors
{
    #region Command classes

    // Command to flush the producer
    public class Flush
    {
    }

    #endregion

    internal class KafkaPublisherFlushActor : ReceiveActor
    {
        private readonly Producer<string, string> producer;


        public KafkaPublisherFlushActor(Producer<string, string> producer)
        {
            this.producer = producer;

            // Commands
            Receive<Flush>(cmd => FlushProducer());
        }


        // Send a message that has not been converted to json
        private void FlushProducer()
        {
            var queueSize = producer.Flush(10);
            DogStatsd.Gauge("QueueOfMsgsToSend", queueSize);
        }
    }
}
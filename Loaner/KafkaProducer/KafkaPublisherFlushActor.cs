using Akka.Actor;
using Akka.Monitoring;
using Confluent.Kafka;
using Loaner.KafkaProducer.Commands;

namespace Loaner.KafkaProducer
{
    #region Command classes

    // Command to flush the producer

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
            Context.Gauge("QueueOfMsgsToSend", queueSize);
        }
    }
}
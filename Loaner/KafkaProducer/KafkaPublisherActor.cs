using System;
using System.Numerics;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using Confluent.Kafka;
using Loaner.KafkaProducer.Commands;
using Newtonsoft.Json;

namespace Loaner.KafkaProducer
{
    #region Command classes

    // The message to be published

    // If a message fails then it is contained in the Resend cmd below

    #endregion

    class KafkaPublisherActor : ReceiveActor
    {
        Producer<string, string> producer;
        private readonly string _topicName;
        ActorSelection thisActor = null;
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private DateTime _lastBootedOn;
        private int _messagesSent = 0;
        private int _messagesReSent = 0;

        public KafkaPublisherActor(string topicName, Producer<string, string> producer, string actorName)
        {
            this.producer = producer;
            _topicName = topicName;

            // Save a reference to this actor for later use in producer callback
            this.thisActor = Context.ActorSelection("/user/" + actorName);

            // Commands
            Receive<Publish>(cmd => PublishMsg(cmd));
            Receive<Resend>(cmd => ResendMsg(cmd));
        }

        private void RegisterStartup()
        {
            _lastBootedOn = DateTime.Now;
        }

        // Send a message that has not been converted to json
        private void PublishMsg(Publish cmd)
        {
            // Convert the msg to JSON before sending
            string json = JsonConvert.SerializeObject(cmd.Msg);
            Send(cmd.Key, json);

            _messagesSent++;

            //_log.Info($"{Self.Path.Name} Sending message {_messagesSent} to kafka");

            // Increment a counter by 1
            Context.IncrementCounter("PublishMsg");
        }


        // Resends a msg, presumably after a failure event
        private void ResendMsg(Resend cmd)
        {
            Send(cmd.Key, cmd.Msg);

            _messagesReSent++;

            //_log.Info($"{Self.Path.Name} ReSending message {_messagesReSent} to kafka");
            // Update telemetry
            Context.IncrementCounter("ResendMsg");
        }


        public void Send(string msgKey, string json)
        {
            try
            {
                var deliveryReport = producer.ProduceAsync(_topicName, msgKey, json);
                deliveryReport.ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        //Context.IncrementCounter("send-SUCCESS");
                        var key = BigInteger.Parse(task.Result.Key);
                        if (key % 1000 == 0)
                        {
                            Console.WriteLine($"Sent key: {task.Result.Key} to kafka @ {DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}");
                        }
                        
                    }
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in Send: {e.Message}");
            }
        }
    }
}
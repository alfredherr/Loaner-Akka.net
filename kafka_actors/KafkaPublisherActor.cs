using System;
using Akka.Actor;
using Confluent.Kafka;
using Newtonsoft.Json;
using StatsdClient;

namespace kafka_actors
{
    #region Command classes

    // The message to be published
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

    // If a message fails then it is contained in the Resend cmd below
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

    #endregion

    internal class KafkaPublisherActor : ReceiveActor
    {
        private readonly string _topicName;
        private readonly Producer<string, string> producer;
        private ActorSelection thisActor;


        public KafkaPublisherActor(string topicName, Producer<string, string> producer, string actorName)
        {
            this.producer = producer;
            _topicName = topicName;

            // Save a reference to this actor for later use in producer callback
            thisActor = Context.ActorSelection("/user/" + actorName);

            // Commands
            Receive<Publish>(cmd => PublishMsg(cmd));
            Receive<Resend>(cmd => ResendMsg(cmd));
        }


        // Send a message that has not been converted to json
        private void PublishMsg(Publish cmd)
        {
            // Convert the msg to JSON before sending
            var json = JsonConvert.SerializeObject(cmd.Msg);
            Send(cmd.Key, json);

            // Increment a counter by 1
            DogStatsd.Increment("PublishMsg");
        }


        // Resends a msg, presumably after a failure event
        private void ResendMsg(Resend cmd)
        {
            Send(cmd.Key, cmd.Msg);

            // Update telemetry
            DogStatsd.Increment("ResendMsg");
        }


        public void Send(string msgKey, string json)
        {
            try
            {
                var deliveryReport = producer.ProduceAsync(_topicName, msgKey, json);
                deliveryReport.ContinueWith(task =>
                {
/*
                    if (task.Result.Error.HasError)
                    {
                        // Update telemetery
                        DogStatsd.Increment("send-ERROR");

                        // TODO Check for the type of error. Local timeout is certainly one that can occur, in which case resend. Check others as 
                        // may be a resend is a bad thing.
                        thisActor.Tell(new Resend(msgKey, json));

                    }
*/

                    if (task.IsCompletedSuccessfully)
                    {
                        DogStatsd.Increment("send-SUCCESS");
                        var key = task.Result.Key;
                        if (key.EndsWith("999999"))
                            Console.WriteLine($"Complete key: {task.Result.Key}");
                        else if (key.EndsWith("0000"))
                            Console.WriteLine($"Sent key: {task.Result.Key}");
                    }

                    //                Console.WriteLine($"Partition: {task.Result.Partition}, Offset: {task.Result.Offset}");
                });
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception in Send: {e.StackTrace}");
            }
        }
    }
}
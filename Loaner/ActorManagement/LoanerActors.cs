using Akka.Actor;
using Confluent.Kafka;

namespace Loaner.ActorManagement
{
    public static class LoanerActors
    {
        public static ActorSystem DemoActorSystem;
        public static IActorRef DemoSystemSupervisor = ActorRefs.Nobody;

        public static IActorRef AccountStatePublisherActor = ActorRefs.Nobody;
        public static IActorRef AccountStateFlushActor = ActorRefs.Nobody;
        public static string AccountStateKafkaTopicName;
        public static Producer<string, string> MyKafkaProducer;

        public static string CommandsToRulesFilename;
        public static string BusinessRulesFilename;

        public const int TakeSystemSupervisorSnapshotAt = 1;
        public const int TakePortolioSnapshotAt = 1;
        public const int TakeAccountSnapshotAt = 1;
    }
}
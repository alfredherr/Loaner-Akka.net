using Akka.Actor;
using Confluent.Kafka;

namespace Loaner.ActorManagement
{
    public static class LoanerActors
    {
        public static ActorSystem DemoActorSystem;
        public static IActorRef DemoSystemSupervisor = ActorRefs.Nobody;


        public static IActorRef AccountStatePublisherActor = ActorRefs.Nobody;
        public static IActorRef FlushActor = ActorRefs.Nobody;
        public static string AccountStateKafkaTopicName;

        public static IActorRef PortfolioStatePublisherActor = ActorRefs.Nobody;
        public static IActorRef PortfolioStateFlushActor = ActorRefs.Nobody;
        public static string PortfolioStateKafkaTopicName;

        public static Producer<string, string> MyKafkaProducer;

        public static string CommandsToRulesFilename;
        public static string BusinessRulesFilename;

        public const int TakeSystemSupervisorSnapshotAt = 1;
        public const int TakePortolioSnapshotAt = 10000; // this must be the number of records you want to load per portfolio (so it only snapshots once)
        public const int TakeAccountSnapshotAt = 1;
    }
}
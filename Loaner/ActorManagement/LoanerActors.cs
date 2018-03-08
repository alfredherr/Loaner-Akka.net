using Akka.Actor;
using Confluent.Kafka;
using Loaner.SnapShotStore3;

namespace Loaner.ActorManagement
{
    public static class LoanerActors
    {
        public const int TakeSystemSupervisorSnapshotAt = 1;

        public const int
            TakePortolioSnapshotAt =
                10_000; // this must be the number of records you want to load per portfolio (so it only snapshots once)

        public const int SnapshotFlushTimer = 5_000; // milliseconds
        public const int TakeAccountSnapshotAt = 1;
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
        
        public static IActorRef AccountBusinessRulesMapperRouter = ActorRefs.Nobody;
        public static IActorRef AccountBusinessRulesHandlerRouter = ActorRefs.Nobody;
    }

//    public class Test
//    {
//        public FileSnapshotStore3 store { get; set; }
//    }
}
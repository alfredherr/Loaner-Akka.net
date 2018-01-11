using Akka.Actor;

namespace Loaner.ActorManagement
{
    public static class LoanerActors
    { 
        public static ActorSystem DemoActorSystem;
        public static IActorRef DemoSystemSupervisor = ActorRefs.Nobody;
        public const int TakeSystemSupervisorSnapshotAt = 1000;
        public const int TakePortolioSnapshotAt = 1000;
        public const int TakeAccountSnapshotAt = 3;

    }


}

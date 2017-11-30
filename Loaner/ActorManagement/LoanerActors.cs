using Akka.Actor;

namespace Loaner.ActorManagement
{
    public static class LoanerActors
    { 
        public static ActorSystem DemoActorSystem;
        public static IActorRef DemoSystemSupervisor = ActorRefs.Nobody;
       
    }


}

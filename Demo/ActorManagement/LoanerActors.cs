﻿using Akka.Actor;

namespace Demo.ActorManagement
{
    public static class LoanerActors
    { 
        public static ActorSystem DemoActorSystem;
        public static IActorRef DemoSystemSupervisor = ActorRefs.Nobody;
        public static int TAKE_PORTFOLIO_SNAPSHOT_AT = 10000;
        public static int TAKE_ACCOUNT_SNAPSHOT_AT = 100;
        public static int TAKE_SNAPSHOT_AT = 1000;
    }


}

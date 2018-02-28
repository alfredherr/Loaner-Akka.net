using System.Linq;
using Akka.Actor;
using Akka.Configuration;

namespace Lighthouse.NetCoreApp
{
    /// <summary>
    ///     Launcher for the Lighthouse <see cref="ActorSystem" />
    /// </summary>
    public static class LighthouseHostFactory
    {
        public static ActorSystem LaunchLighthouse(string ipAddress = null, int? specifiedPort = null)
        {
            var systemName = "lighthouse";
            var clusterConfig = GetConfig();

            var lighthouseConfig = clusterConfig.GetConfig("lighthouse");
            if (lighthouseConfig != null) systemName = lighthouseConfig.GetString("actorsystem", systemName);

            var remoteConfig = clusterConfig.GetConfig("akka.remote");
            ipAddress = ipAddress ??
                        remoteConfig.GetString("dot-netty.tcp.public-hostname") ??
                        "127.0.0.1"; //localhost as a final default
            var port = specifiedPort ?? remoteConfig.GetInt("dot-netty.tcp.port");

            if (port == 0)
                throw new ConfigurationException(
                    "Need to specify an explicit port for Lighthouse. Found an undefined port or a port value of 0 in App.config.");

            var selfAddress = string.Format("akka.tcp://{0}@{1}:{2}", systemName, ipAddress, port);
            var seeds = clusterConfig.GetStringList("akka.cluster.seed-nodes");
            if (!seeds.Contains(selfAddress)) seeds.Add(selfAddress);

            var injectedClusterConfigString = seeds.Aggregate("akka.cluster.seed-nodes = [",
                (current, seed) => current + @"""" + seed + @""", ");
            injectedClusterConfigString += "]";

            var finalConfig = ConfigurationFactory.ParseString(
                    string.Format(@"akka.remote.dot-netty.tcp.public-hostname = {0} 
akka.remote.dot-netty.tcp.port = {1}", ipAddress, port))
                .WithFallback(ConfigurationFactory.ParseString(injectedClusterConfigString))
                .WithFallback(clusterConfig);

            return ActorSystem.Create(systemName, finalConfig);
        }

        private static Config GetConfig()
        {
            var configString = @"
                    
                    lighthouse{
		                    actorsystem: ""demoSystem"" #POPULATE NAME OF YOUR ACTOR SYSTEM HERE
	                    }
			
                    akka {
	                    actor { 
		                    provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
	                    }
                        serializers {
                            hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                        }
                        serialization-bindings { 
                            ""System.Object"" = hyperion 
                        }
                
						
	                    remote {
		                    log-remote-lifecycle-events = INFO
		                    dot-netty.tcp {
			                    transport-class = ""Akka.Remote.Transport.DotNetty.TcpTransport, Akka.Remote""
			                    applied-adapters = []
			                    transport-protocol = tcp
			                    #will be populated with a dynamic host-name at runtime if left uncommented
                                            public-hostname = ""lighthouse""
			                    hostname = ""lighthouse""
			                    port = 4053
		                    }
	                    }     
											
	                    cluster {
		                    #will inject this node as a self-seed node at run-time
		                    # manually populate other seed nodes here, i.e.
                                    # ""akka.tcp://lighthouse@127.0.0.1:4053"", ""akka.tcp://lighthouse@127.0.0.1:4044""
		                    seed-nodes = []
		                    roles = [lighthouse]
	                    }
                    }
            ";
            return ConfigurationFactory.ParseString(configString);
        }
    }
}
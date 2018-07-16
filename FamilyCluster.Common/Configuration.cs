namespace FamilyCluster.Common
{
    public class AppConfiguration
    {
        public static string GetAkkaConfiguration(string id)
        {
            return @"

					    akka {
            actor {             
            serializers {
                hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
            }  
                  
            serialization-bindings {
                ""System.Object"" = hyperion
            }
                 
            provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
    
            debug {  
                receive = on 
                autoreceive = on
                lifecycle = on
                event-stream = on
                unhandled = on
            }
            deployment {
               /" + id + @" {
                    router = broadcast-pool                    
                    nr-of-instances = 10,                    
                    cluster{
                        enabled = on
                        allow-local-routees = on
                        max-nr-of-instances-per-node = 1
                        use-role=blockchain
                    }
                }
            }
        }
        remote {
            log-remote-lifecycle-events = DEBUG
            helios.tcp {
                port = 0
                hostname = localhost  
                maximum-frame-size = 12800000b
            }
        }
        cluster {
            seed-nodes = [""akka.tcp://FamilyCluster@127.0.0.1:4053""] 
            roles = [""blockchain""]
        }
    }
";
        }
    }
}
using Akka.Actor;
using Akka.Configuration;

class Program
{
    static void Main(string[] args)
    {
        try 
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Prosím zadejte port jako argument");
                return;
            }

            if (!int.TryParse(args[0], out int port))
            {
                Console.WriteLine("Neplatný port");
                return;
            }

            var config = ConfigurationFactory.ParseString($@"
                akka {{
                    actor {{
                        provider = remote
                        serializers {{
                            hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                        }}
                        serialization-bindings {{
                            ""System.Object"" = hyperion
                        }}
                        serialization-settings {{
                            hyperion {{
                                preserveObjectReferences = true
                            }}
                        }}
                        log-dead-letters = off
                        log-dead-letters-during-shutdown = off
                    }}
                    remote.dot-netty.tcp {{
                        hostname = ""127.0.0.1""
                        port = {port}
                        message-frame-size = 30000000b
                        send-buffer-size = 30000000b
                        receive-buffer-size = 30000000b
                        maximum-frame-size = 30000000b
                    }}
                }}");

            using var system = ActorSystem.Create("WorkerSystem", config);
            var worker = system.ActorOf(Props.Create<WorkerActor>(), "worker");
            
            Console.WriteLine($"Worker spuštěn na portu {port}");
            Console.WriteLine($"Worker path: {worker.Path}");
            Console.WriteLine("Stiskněte Enter pro ukončení...");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při spuštění workera: {ex.Message}");
        }
    }
}
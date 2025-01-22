using Akka.Actor;
using Akka.Configuration;
using Microsoft.AspNetCore.Builder;

var config = ConfigurationFactory.ParseString(@"
    akka {
        actor {
            provider = remote
            serializers {
                hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
            }
            serialization-bindings {
                ""System.Object"" = hyperion
            }
            serialization-settings {
                hyperion {
                    preserveObjectReferences = true
                }
            }
            log-dead-letters = off
            log-dead-letters-during-shutdown = off
        }
        remote.dot-netty.tcp {
            hostname = ""127.0.0.1""
            port = 8081
            message-frame-size = 30000000b
            send-buffer-size = 30000000b
            receive-buffer-size = 30000000b
            maximum-frame-size = 30000000b
        }
    }");

var system = ActorSystem.Create("PrimeCalculator", config);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(provider =>
{
    var workerAddresses = new List<string>
    {
        "akka.tcp://WorkerSystem@127.0.0.1:8082/user/worker",
        "akka.tcp://WorkerSystem@127.0.0.1:8083/user/worker",
    };

    var master = system.ActorOf(Props.Create(() => new MasterActor(workerAddresses)), "master");
    return master;
});

builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
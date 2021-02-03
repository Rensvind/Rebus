using System;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Backoff;
using Rebus.Config;
using Rebus.Outbox.Common.Config;
using Rebus.Outbox.Handler.Config;
using Rebus.Outbox.Handler.SqlServer.Config;
using Rebus.Outbox.SqlServer.Common.Config;
using Rebus.Retry.Simple;

namespace OutboxTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = "connectionString";

            using (var activator = new BuiltinHandlerActivator())
            {
                activator.Handle<bool>((b, x) =>
                {
                    b.SendLocal($"Sending {x} from bool handler");
                    return Task.CompletedTask;
                });

                activator.Handle<string>(x => Task.CompletedTask);


                var bus = Configure
                    .With(activator)
                    .Transport(t => t.UseRabbitMq("amqp://localhost", "test")
                        .InputQueueOptions(queueConfig => queueConfig.AddArgument("x-queue-type", "quorum"))
                        .DefaultQueueOptions(queueConfig => queueConfig.AddArgument("x-queue-type", "quorum"))
                    )
                    .Outbox(o => 
                        o.UseSqlServer(connectionString)
                            .ForHandlers(t => t.Config("Rebus.Outbox", true))
                    )
                    .Options(o =>
                    {
                        o.SimpleRetryStrategy(errorQueueAddress:"errorquorom");
                        o.LogPipeline(true);
                    }).Start();


                Console.WriteLine("Anything but 'quit' sends a message");
                while (Console.ReadLine() != "quit")
                {
                    await bus.SendLocal(true);
                }
            }
        }
    }
}
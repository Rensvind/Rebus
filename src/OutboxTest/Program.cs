using System;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Backoff;
using Rebus.Config;
using Rebus.Outbox;
using Rebus.Outbox.SqlServer;
using Rebus.Retry.Simple;

namespace OutboxTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = "<connectionString>";

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
                    .Outbox(o => o.UseSqlServer(connectionString, "Rebus.Outbox", true))
                    .Options(o =>
                    {
                        o.SimpleRetryStrategy(errorQueueAddress:"errorquorom");
                        
                        o.LogPipeline(true);
                        
                        o.SetMaxParallelism(20);
                        o.SetNumberOfWorkers(2);
                        o.SetBackoffTimes(TimeSpan.FromMilliseconds(100));
                        
                        // Could we somehow do this configuration in SqlServerOutboxStorageConfigurationExtensions ?
                        o.Register(rx => new DbConnectionAccessor());
                        o.Register<IOutboxTransactionFactory>(rx => new SqlServerOutboxTransactionFactory(connectionString, rx.Get<DbConnectionAccessor>()));

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
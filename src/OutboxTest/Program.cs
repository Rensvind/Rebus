using System;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Outbox;
using Rebus.Outbox.SqlServer;

namespace OutboxTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connectionString = "";
            using (var activator = new BuiltinHandlerActivator())
            {
                activator.Handle<bool>((bus, x) =>
                {
                    Console.WriteLine(x.ToString());
                    bus.SendLocal($"Sending {x} from bool handler");
                    return Task.CompletedTask;
                });

                activator.Handle<string>(x =>
                {
                    Console.WriteLine(x);
                    return Task.CompletedTask;
                });

                
                var bus = Configure
                    .With(activator)
                    .Transport(t => t.UseRabbitMq("amqp://localhost", "test"))
                    .Outbox(o => o.UseSqlServer(connectionString, "Rebus.Outbox", true))
                    .Options(o =>
                    {
                        o.LogPipeline(true);
                        
                        o.SetNumberOfWorkers(1);
                        o.SetMaxParallelism(1);

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
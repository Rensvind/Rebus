using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using HttpOutbox.Handlers;
using Outbox.Http;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Outbox;
using Rebus.Outbox.SqlServer;
using Rebus.Retry.Simple;
using Rebus.ServiceProvider;
using Rebus.Transport;

namespace HttpOutbox
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AutoRegisterHandlersFromAssemblyOf<TestMessageHandler>();

            var dbConnectionAccessor = new DbConnectionAccessor();
            
            services.AddSingleton(dbConnectionAccessor);

            services.AddSingleton<IOutboxStorage>(new SqlServerOutboxStorage(new SqlServerOutboxSettings
            {
                TableName = "Rebus.Outbox"
            }, dbConnectionAccessor, "testHttp"));

            var connectionString = "<connectionString>";

            services.AddSingleton<IOutboxTransactionFactory>(new SqlServerOutboxTransactionFactory(connectionString, dbConnectionAccessor));

            // Configure and register Rebus
            services.AddRebus((configure, sp) => configure
                .Logging(l => l.ColoredConsole())
                .Transport(t => t.UseRabbitMq("amqp://localhost", "testHttp")
                    .InputQueueOptions(queueConfig => queueConfig.AddArgument("x-queue-type", "quorum"))
                    .DefaultQueueOptions(queueConfig => queueConfig.AddArgument("x-queue-type", "quorum"))
                )
                .Outbox(o => o.UseSqlServer(connectionString, "Rebus.Outbox", true))
                .Options(opts =>
                {
                    opts.Decorate(c =>
                    {
                        var transport = c.Get<ITransport>();

                        var outboxMessagesProcessor = new OutboxMessagesProcessor(
                            10,
                            transport,
                            c.Get<IOutboxStorage>(),
                            TimeSpan.FromSeconds(5),
                            c.Get<IRebusLoggerFactory>(),
                            c.Get<IOutboxTransactionFactory>(),
                            c.Get<CancellationToken>());

                        outboxMessagesProcessor.Run();

                        return transport;
                    });
                    
                    opts.SimpleRetryStrategy(errorQueueAddress: "errorquorom");
                    opts.Register(_ => dbConnectionAccessor);
                    opts.Register(rc => sp.GetService<IOutboxTransactionFactory>());
                }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.ApplicationServices.UseRebus();
            
            app.UseRouting();

            app.UseAuthorization();

            app.UseMiddleware<OutboxMiddleware>();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    
}

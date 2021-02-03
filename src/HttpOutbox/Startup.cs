using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HttpOutbox.Handlers;
using Rebus.Config;
using Rebus.Outbox.Common.Config;
using Rebus.Outbox.Handler.Config;
using Rebus.Outbox.Handler.SqlServer.Config;
using Rebus.Outbox.Http;
using Rebus.Outbox.Http.Config;
using Rebus.Outbox.Http.SqlServer.Config;
using Rebus.Outbox.SqlServer.Common.Config;
using Rebus.Retry.Simple;
using Rebus.ServiceProvider;

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

            var connectionString = "connectionString";

            services.AddHttpOutbox("apXlTbqr1", "Rebus.HttpOutbox", connectionString);

            // Configure and register Rebus
            services.AddRebus((configure, sp) => configure
                .Logging(l => l.ColoredConsole())
                .Transport(t =>
                    t.UseRabbitMq("amqp://localhost", "testHttp")
                    .InputQueueOptions(queueConfig => queueConfig.AddArgument("x-queue-type", "quorum"))
                    .DefaultQueueOptions(queueConfig => queueConfig.AddArgument("x-queue-type", "quorum")
                    )
                )
                .Outbox(o =>
                    o.UseSqlServer(connectionString)
                        .ForHandlers(t => t.Config("Rebus.Outbox", true))
                        .ForHttp(sp.GetService<IOutboxHttpStorage>())
                )
                .Options(opts =>
                {
                    opts.LogPipeline(true);
                    opts.SimpleRetryStrategy(errorQueueAddress: "errorquorom");
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

            app.UseOutbox();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    
}

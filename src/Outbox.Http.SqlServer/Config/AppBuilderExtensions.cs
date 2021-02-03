using Microsoft.AspNetCore.Builder;

namespace Rebus.Outbox.Http.SqlServer.Config
{
    public static class AppBuilderExtensions
    {
        public static void UseOutbox(this IApplicationBuilder app)
        {
            app.UseMiddleware<OutboxMiddleware>();
        }
    }
}
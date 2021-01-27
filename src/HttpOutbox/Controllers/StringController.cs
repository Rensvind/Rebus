using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using HttpOutbox.Handlers;
using Rebus.Bus;
using Rebus.Outbox.SqlServer;
using Rebus.Transport;

namespace HttpOutbox.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StringController : ControllerBase
    {
        private readonly IBus bus;
        private readonly DbConnectionAccessor accessor;

        public StringController(IBus bus, DbConnectionAccessor accessor)
        {
            this.bus = bus;
            this.accessor = accessor;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            var dbConnectionAndTransactionWrapper = accessor.Item;

            await bus.SendLocal(new TestMessage
            {
                TheMessage = $"I was sent {DateTime.UtcNow}"
            });

            return "Message sent";
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HttpOutbox.Handlers;
using Rebus.Bus;
using Rebus.Transport;

namespace HttpOutbox.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IBus bus;

        public TestController(IBus bus)
        {
            this.bus = bus;
        }

        [HttpGet]
        public async Task<string> Get(int burst = 10)
        {
            await Task.WhenAll(Enumerable.Range(0, burst).Select(x => bus.SendLocal(new TestMessage
            {
                TheMessage = $"I was sent {DateTime.UtcNow}"
            })));

            return "Message sent";
        }
    }
}

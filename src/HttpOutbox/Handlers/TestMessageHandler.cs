using System;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace HttpOutbox.Handlers
{
    public class TestMessageHandler : IHandleMessages<TestMessage>
    {
        public IBus Bus { get; set; }
        public Task Handle(TestMessage message)
        {
            Console.WriteLine($"I received the message '{message.TheMessage}'.");
            return Task.CompletedTask;
        }
    }

    public class TestMessage
    {
        public string TheMessage { get; set; }
    }
}

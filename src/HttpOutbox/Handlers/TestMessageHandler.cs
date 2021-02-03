using System;
using System.Threading.Tasks;
using Rebus.Handlers;

namespace HttpOutbox.Handlers
{
    public class TestMessageHandler : IHandleMessages<TestMessage>
    {
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

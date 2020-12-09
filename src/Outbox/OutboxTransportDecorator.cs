using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Outbox
{
    internal class OutboxTransportDecorator : ITransport
    {
        internal const string OutgoingMessagesItemsKey = "outbox-outgoing-messages";
        private readonly ITransport transport;

        public OutboxTransportDecorator(ITransport transport)
        {
            this.transport = transport;
        }

        public void CreateQueue(string address)
        {
            transport.CreateQueue(address);
        }

        public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            var outgoingMessages = context.GetOrAdd(OutgoingMessagesItemsKey, () =>
            {
                var messages = new ConcurrentQueue<TransportMessage>();
                return messages;
            });

            message.Headers.Add(OutboxHeaders.Recipient, destinationAddress);
            outgoingMessages.Enqueue(message);

            // Note: What will happen to messages that are sent directly and not added to the in-memory list that is sent at ITransactionContext commit/complete ?
            return transport.Send(destinationAddress, message, context);
        }

        public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            return transport.Receive(context, cancellationToken);
        }

        public string Address => transport.Address;
    }
}
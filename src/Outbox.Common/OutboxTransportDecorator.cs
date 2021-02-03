using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Outbox.Common
{
    public class OutboxTransportDecorator : ITransport
    {
        private readonly ITransport transport;

        protected internal OutboxTransportDecorator(ITransport transport)
        {
            this.transport = transport;
        }

        public void CreateQueue(string address)
        {
            transport.CreateQueue(address);
        }

        public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
        {
            if (!context.GetOrAdd(OutboxConstants.OutboxShouldHandle, () => false)) 
            {
                transport.Send(destinationAddress, message, context);
                return Task.CompletedTask;
            }

            var outgoingMessages = context.GetOrAdd(OutboxConstants.OutgoingMessagesItemsKey, () =>
            {
                var messages = new ConcurrentQueue<TransportMessage>();

                context.OnCommitted(tc =>
                {
                    return Task.WhenAll(messages.Where(x => x.Body.Length > 0).Select(transportMessage =>
                    {
                        var address = transportMessage.Headers.GetValue(OutboxHeaders.Recipient);
                        transportMessage.Headers.Remove(OutboxHeaders.Recipient);
                        return transport.Send(address, transportMessage, tc);
                    }).ToArray());

                });
                
                return messages;
            });

            if(!message.Headers.ContainsKey(OutboxHeaders.Recipient))
                message.Headers.Add(OutboxHeaders.Recipient, destinationAddress);
            outgoingMessages.Enqueue(message);

            return Task.CompletedTask;
        }

        public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
        {
            return transport.Receive(context, cancellationToken);
        }

		public string Address => transport.Address;
    }
}
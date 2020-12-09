using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Outbox
{
    public class PessemisticOutboxStep : IIncomingStep
    {
        private readonly IOutboxStorage outboxStorage;
        private readonly ITransport transport;

        public PessemisticOutboxStep(IOutboxStorage outboxStorage, ITransport transport)
        {
            this.outboxStorage = outboxStorage;
            this.transport = transport;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var transactionContext = context.Load<ITransactionContext>();
            var message = context.Load<Message>();

            List<TransportMessage> outgoingMessages;

            if ((outgoingMessages = await outboxStorage.GetOutgoingMessages(message)) != null)
            {
                if (!outgoingMessages.Any())
                    return;
                await Task.WhenAll(outgoingMessages.Select(x => SendInternal(transport, x, transactionContext)));
            }
            else
            {
                await next();
                outgoingMessages = transactionContext.GetOrNull<ConcurrentQueue<TransportMessage>>(OutboxTransportDecorator.OutgoingMessagesItemsKey)?.ToList() ?? new List<TransportMessage>();
                await outboxStorage.TryStore(message, outgoingMessages);
            }
        }

        private static Task SendInternal(ITransport transport, TransportMessage message, ITransactionContext transactionContext)
        {
            var destinationAddress = message.Headers[OutboxHeaders.Recipient];
            message.Headers.Remove(OutboxHeaders.Recipient);
            return transport.Send(destinationAddress, message, transactionContext);
        }
    }
}
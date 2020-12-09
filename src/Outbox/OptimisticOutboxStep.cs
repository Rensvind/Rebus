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
    public class OptimisticOutboxStep : IIncomingStep
    {
        private readonly IOutboxStorage outboxStorage;
        private readonly ITransport transport;

        public OptimisticOutboxStep(IOutboxStorage outboxStorage, ITransport transport)
        {
            this.outboxStorage = outboxStorage;
            this.transport = transport;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            await next();
            var transactionContext = context.Load<ITransactionContext>();
            var outgoingMessages = transactionContext.GetOrNull<ConcurrentQueue<TransportMessage>>(OutboxTransportDecorator.OutgoingMessagesItemsKey)?.ToList() ?? new List<TransportMessage>();
            if (await outboxStorage.TryStore(message, outgoingMessages) == false) // False means that the message has already been handled
            {
                // Here we need to remove the current in-memory messages to send before we try to send out the ones we fetch from the outbox
                // How can we clear the concurrent dictionary ?
                var dic = transactionContext.GetOrNull<object>("rabbitmq-outgoing-messages");
                dic.GetType().GetMethod("Clear").Invoke(dic, null);

                outgoingMessages = await outboxStorage.GetOutgoingMessages(message);
                if (!outgoingMessages.Any())
                    return;
                await Task.WhenAll(outgoingMessages.Select(x => SendInternal(transport, x, transactionContext)));
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
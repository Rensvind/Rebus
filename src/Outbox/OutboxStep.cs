using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Rebus.Extensions;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.Outbox
{
    public class OutboxStep : IIncomingStep
    {
        private readonly IOutboxStorage outboxStorage;
        private readonly ITransport transport;
        private readonly IOutboxTransactionFactory outboxTransactionFactory;

        public OutboxStep(IOutboxStorage outboxStorage, ITransport transport, IOutboxTransactionFactory outboxTransactionFactory)
        {
            this.outboxStorage = outboxStorage;
            this.transport = transport;
            this.outboxTransactionFactory = outboxTransactionFactory;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            var message = context.Load<TransportMessage>();
            var txContext = context.Load<ITransactionContext>();
            
            using (var tx = outboxTransactionFactory.Start())
            {
                var outgoingMessages = await outboxStorage.GetOutgoingMessages(message);

                if (!outgoingMessages.Any())
                {
                    await transport.Send(OutboxConstants.DummyValue, new TransportMessage(new Dictionary<string, string>(), Array.Empty<byte>()), txContext);
                    await next();
                    await PersistOutgoingMessages(txContext, outboxStorage, message);
                }
                else
                {
                    await Task.WhenAll(outgoingMessages.Where(x => !x.Headers.ContainsKey(OutboxConstants.DummyValue)).Select(x =>
                        transport.Send(x.Headers.GetValue(OutboxHeaders.Recipient), x, txContext)));
                }
                
                await tx.CompleteAsync();
            }

            txContext.OnCommitted(async _ =>
            {
                using var tx = outboxTransactionFactory.Start();
                await outboxStorage.DeleteOutgoingMessages(message);
                await tx.CompleteAsync();
            });
            
            txContext.OnCompleted(async _ =>
            {
                using var tx = outboxTransactionFactory.Start();
                await outboxStorage.DeleteIdempotencyCheckMessage(message);
                await tx.CompleteAsync();
            });
        }

        private static Task PersistOutgoingMessages(ITransactionContext transactionContext, IOutboxStorage outboxStorage, TransportMessage message)
        {
            var outgoingMessages = transactionContext.GetOrThrow<ConcurrentQueue<TransportMessage>>(OutboxConstants.OutgoingMessagesItemsKey);
            return outboxStorage.Store(message, outgoingMessages);
        }
    }
}
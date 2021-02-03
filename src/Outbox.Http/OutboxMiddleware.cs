using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rebus.Messages;
using Rebus.Outbox.Common;
using Rebus.Transport;

namespace Rebus.Outbox.Http
{
    public class OutboxMiddleware
    {
        private readonly RequestDelegate next;

        public OutboxMiddleware(RequestDelegate next)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context, IOutboxHttpStorage outboxStorage, IOutboxTransactionFactory outboxTransactionFactory)
        {
            ConcurrentQueue<TransportMessage> outgoingMessages;

            using (var rebusTx = new RebusTransactionScope())
            {
                rebusTx.UseOutbox();

                using (var tx = outboxTransactionFactory.Start())
                {
                    await next(context);

                    outgoingMessages = rebusTx.TransactionContext.GetOrNull<ConcurrentQueue<TransportMessage>>(OutboxConstants.OutgoingMessagesItemsKey);

                    if (outgoingMessages != null)
                        await outboxStorage.Store(context.TraceIdentifier, outgoingMessages);

                    await tx.CompleteAsync();
                }

                await rebusTx.TransactionContext.Commit();

                await rebusTx.CompleteAsync();
            }

            if (outgoingMessages == null)
                return;

            using (var scope = new RebusTransactionScope())
            {
                using (var tx = outboxTransactionFactory.Start())
                {
                    await outboxStorage.DeleteOutgoingMessages(context.TraceIdentifier);
                    await tx.CompleteAsync();
                }

                await scope.CompleteAsync();
            }
        }
    }

    public static class Extensions
    {
        public static void UseOutbox(this RebusTransactionScope scope)
        {
            scope.TransactionContext.Items[OutboxConstants.OutboxShouldHandle] = true;
        }
    }
}

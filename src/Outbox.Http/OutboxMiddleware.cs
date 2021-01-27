using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rebus.Messages;
using Rebus.Outbox;
using Rebus.Transport;

namespace Outbox.Http
{
    public class OutboxMiddleware
    {
        private readonly RequestDelegate next;

        public OutboxMiddleware(RequestDelegate next)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context, IOutboxStorage outboxStorage, IOutboxTransactionFactory outboxTransactionFactory)
        {
            ConcurrentQueue<TransportMessage> outgoingMessages;

            using (var rebusTx = new RebusTransactionScope())
            {
                rebusTx.TransactionContext.Items["httpRequest"] = true;
                
                using (var tx = outboxTransactionFactory.Start())
                {
                    await next(context);

                    outgoingMessages = rebusTx.TransactionContext
                        .GetOrNull<ConcurrentQueue<TransportMessage>>("outbox-outgoing-messages");

                    if (outgoingMessages != null)
                        await outboxStorage.Store(context.TraceIdentifier, outgoingMessages);

                    await tx.CompleteAsync();
                }

                await rebusTx.TransactionContext.Commit();
                
                await rebusTx.CompleteAsync();
            }

            if (outgoingMessages == null)
                return;

            using (var tx = outboxTransactionFactory.Start())
            {
                await outboxStorage.DeleteOutgoingMessages(context.TraceIdentifier);
                await tx.CompleteAsync();
            }
        }
    }
}

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

        private static int Val = 1;
        
        // NOTE: Duplicate in OutboxTransportDecorator
        internal const string OutgoingMessagesItemsKey = "outbox-outgoing-messages";
        internal const string DummyValue = "<dummy>";

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

            await using (var tx = outboxTransactionFactory.Start())
            {
                var outgoingMessages = await outboxStorage.GetOutgoingMessages(message);

                if (!outgoingMessages.Any())
                {
                    await transport.Send(DummyValue, new TransportMessage(new Dictionary<string, string>(), Array.Empty<byte>()), txContext);
                    await next();
                    await PersistOutgoingMessages(txContext, outboxStorage, message);
                }
                else
                {
                    await Task.WhenAll(outgoingMessages.Where(x => !x.Headers.ContainsKey(DummyValue)).Select(x =>
                        transport.Send(x.Headers.GetValue(OutboxHeaders.Recipient), x, txContext)));
                }
                
                await tx.CompleteAsync();
            }


            #region Send
            txContext.OnCommitted(async tc =>
            {
                Console.WriteLine("OutboxStep - OnCommitted");
                tc.Items["tx"] = outboxTransactionFactory.Start();

                //await using var tx = /*outboxTransactionFactory.Start();*/
                await outboxStorage.DeleteOutgoingMessages(message);
                //await tx.CompleteAsync();

                
                //if (Val == 1)
                //{
                //    Val++;
                //    throw new ApplicationException("Nehepp");

                //}

                var txx = (IOutboxTransaction)tc.Items["tx"];
                await txx.CompleteAsync();
                await txx.DisposeAsync();
            });
            #endregion
            
            txContext.OnAborted(async tc =>
            {
                var txx = (IOutboxTransaction)tc.Items["tx"];
                await txx.DisposeAsync();
            });
            
            txContext.OnCompleted(async tc =>
            {
                await using var tx = outboxTransactionFactory.Start();
                await outboxStorage.DeleteIdempotencyCheckMessage(message);
                await tx.CompleteAsync();
            });

        }

        private static Task PersistOutgoingMessages(ITransactionContext transactionContext, IOutboxStorage outboxStorage, TransportMessage message)
        {
            var outgoingMessages = transactionContext.GetOrThrow<ConcurrentQueue<TransportMessage>>(OutgoingMessagesItemsKey);
            return outboxStorage.Store(message, outgoingMessages);
        }
    }
}
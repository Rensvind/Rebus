using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Logging;
using Rebus.Outbox.Common;
using Rebus.Transport;

namespace Rebus.Outbox.Http
{
	public class OutboxMessagesProcessor
	{
        private readonly int topMessagesToRetrieve;
        private readonly ITransport transport;
		private readonly IOutboxHttpStorage outboxStorage;
        private readonly TimeSpan pollingInterval;
        private readonly TimeSpan sendMessagesOlderThan;
        private readonly IOutboxTransactionFactory outboxTransactionFactory;
        private readonly CancellationToken busDisposalCancellationToken;
		private readonly ILog log;

		public OutboxMessagesProcessor(
            int topMessagesToRetrieve,
            TimeSpan pollingInterval,
            TimeSpan sendMessagesOlderThan,
			ITransport transport,
			IOutboxHttpStorage outboxStorage,
			
			IRebusLoggerFactory rebusLoggerFactory,
            IOutboxTransactionFactory outboxTransactionFactory,
			CancellationToken busDisposalCancellationToken)
		{
            this.topMessagesToRetrieve = topMessagesToRetrieve;
            this.transport = transport;
			this.outboxStorage = outboxStorage;
            this.pollingInterval = pollingInterval;
            this.sendMessagesOlderThan = sendMessagesOlderThan;
            this.outboxTransactionFactory = outboxTransactionFactory;
            this.busDisposalCancellationToken = busDisposalCancellationToken;
			log = rebusLoggerFactory.GetLogger<OutboxMessagesProcessor>();
		}

		private async Task ProcessOutboxMessages()
		{
			log.Debug("Starting outbox messages processor");

			while (!busDisposalCancellationToken.IsCancellationRequested)
			{
				try
				{
                    using (var scope = new RebusTransactionScope())
                    {
                        using (var tx = outboxTransactionFactory.Start())
                        {
                            var messages =
                                await outboxStorage.GetUnsentOutgoingMessages(topMessagesToRetrieve,
                                    sendMessagesOlderThan);
                            if (messages.Count > 0)
                            {
                                foreach (var message in messages)
                                {
                                    var destinationAddress = message.Headers[OutboxHeaders.Recipient];
                                    message.Headers.Remove(OutboxHeaders.Recipient);
                                    await transport.Send(destinationAddress, message, scope.TransactionContext);
                                }
                            }

                            await tx.CompleteAsync();
                        }

                        await scope.CompleteAsync();
                    }

                    await Task.Delay(pollingInterval, busDisposalCancellationToken);
				}
				catch (OperationCanceledException) when (busDisposalCancellationToken.IsCancellationRequested)
				{
					// we're shutting down
				}
				catch (Exception exception)
				{
					log.Error(exception, "Unhandled exception in outbox messages processor");
				}
			}

			log.Debug("Outbox messages processor stopped");
		}

		public Task Run() => Task.Run(ProcessOutboxMessages);
	}
}

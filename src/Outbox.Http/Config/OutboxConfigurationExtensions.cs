using System;
using System.Threading;
using Rebus.Config;
using Rebus.Logging;
using Rebus.Outbox.Common;
using Rebus.Transport;

namespace Rebus.Outbox.Http.Config
{
    public static class OutboxConfigurationExtensions
    {
        /// <summary>
        /// Enables OutboxMessagesProcessor needed for outbox http request support
        /// </summary>
        /// <param name="configurer"></param>
        /// <param name="pollingInterval">The polling frequency (default value is 5 seconds)</param>
        /// <param name="messagesOlderThan">Only fetch messages older than (default value is 2 seconds)</param>
        /// <param name="messagesToRetrieve">Max number of messages to handle per polling</param>
        /// <returns></returns>
        public static StandardConfigurer<IOutboxTransactionFactory> ForHttp(this StandardConfigurer<IOutboxTransactionFactory> configurer, TimeSpan? pollingInterval = default, TimeSpan? messagesOlderThan = default,  int messagesToRetrieve = 10)
        {
            pollingInterval ??= TimeSpan.FromSeconds(5);
            messagesOlderThan ??= TimeSpan.FromSeconds(2);
            
            if (pollingInterval.Value.TotalMilliseconds < 100)
                throw new ArgumentOutOfRangeException(nameof(pollingInterval), "Must be greater than or equal to 100 milliseconds");

            if (messagesOlderThan.Value.TotalSeconds < 1)
                throw new ArgumentOutOfRangeException(nameof(messagesOlderThan), "Must be greater that or equal to 1 second");

            if (messagesToRetrieve < 1)
                throw new ArgumentOutOfRangeException(nameof(messagesToRetrieve), "At least one message must be fetched");

            
            // This is ugly, is there some other way to do this?
            configurer.OtherService<ITransport>().Decorate(c =>
            {
                var transport = c.Get<ITransport>();
                
                var outboxMessagesProcessor = new OutboxMessagesProcessor(
                    messagesToRetrieve,
                    pollingInterval.Value,
                    messagesOlderThan.Value,
                    transport,
                    c.Get<IOutboxHttpStorage>(),
                    c.Get<IRebusLoggerFactory>(),
                    c.Get<IOutboxTransactionFactory>(),
                    c.Get<CancellationToken>());

                outboxMessagesProcessor.Run();


                return transport;
            });
            
            return configurer;
        }
    
        /// <summary>
        /// Enables OutboxMessagesProcessor needed for outbox http request support
        /// </summary>
        /// <param name="configurer"></param>
        /// <param name="pollingInterval">The polling frequency (default value is 5 seconds)</param>
        /// <param name="messagesOlderThan">Only fetch messages older than (default value is 2 seconds)</param>
        /// <param name="messagesToRetrieve">Max number of messages to handle per polling</param>
        /// <returns></returns>
        public static StandardConfigurer<IOutboxTransactionFactory> ForHttp(this StandardConfigurer<IOutboxTransactionFactory> configurer, IOutboxHttpStorage outboxHttpStorage, TimeSpan? pollingInterval = default, TimeSpan? messagesOlderThan = default, int messagesToRetrieve = 10)
        {
            pollingInterval ??= TimeSpan.FromSeconds(5);
            messagesOlderThan ??= TimeSpan.FromSeconds(2);

            if (pollingInterval.Value.TotalMilliseconds < 100)
                throw new ArgumentOutOfRangeException(nameof(pollingInterval), "Must be greater than or equal to 100 milliseconds");

            if (messagesOlderThan.Value.TotalSeconds < 1)
                throw new ArgumentOutOfRangeException(nameof(messagesOlderThan), "Must be greater that or equal to 1 second");

            if (messagesToRetrieve < 1)
                throw new ArgumentOutOfRangeException(nameof(messagesToRetrieve), "At least one message must be fetched");

            configurer.OtherService<ITransport>().Decorate(c =>
            {
                var transport = c.Get<ITransport>();

                var outboxMessagesProcessor = new OutboxMessagesProcessor(
                    messagesToRetrieve,
                    pollingInterval.Value,
                    messagesOlderThan.Value,
                    transport,
                    outboxHttpStorage,
                    c.Get<IRebusLoggerFactory>(),
                    c.Get<IOutboxTransactionFactory>(),
                    c.Get<CancellationToken>());

                outboxMessagesProcessor.Run();


                return transport;
            });

            return configurer;
        }
    }
}
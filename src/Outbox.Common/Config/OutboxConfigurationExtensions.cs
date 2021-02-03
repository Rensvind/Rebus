using System;
using Rebus.Config;
using Rebus.Transport;

namespace Rebus.Outbox.Common.Config
{
	/// <summary>
    /// Configuration extensions to use transactional outbox for transport
    /// </summary>
    public static class OutboxConfigurationExtensions
    {
        /// <summary>
        /// Decorates transport to save messages into an outbox instead of sending them directly
        /// </summary>
        /// <param name="configurer"></param>
        /// <param name="outboxTransactionFactoryConfigurer"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static RebusConfigurer Outbox(this RebusConfigurer configurer,
            Action<StandardConfigurer<IOutboxTransactionFactory>> outboxTransactionFactoryConfigurer)
        {
            configurer
                .Transport(t =>
                {
                    t.Decorate(c =>
                    {
                        var transport = c.Get<ITransport>();
                        return new OutboxTransportDecorator(transport);
                    });

                    if (outboxTransactionFactoryConfigurer == null)
                        throw new ArgumentNullException(nameof(outboxTransactionFactoryConfigurer));

                    outboxTransactionFactoryConfigurer(t.OtherService<IOutboxTransactionFactory>());

                });
            return configurer;
        }
    }
}
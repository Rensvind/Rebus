using System;
using Rebus.Config;
using Rebus.Outbox.Common;
using Rebus.Pipeline;
using Rebus.Retry.Simple;
using Rebus.Transport;

namespace Rebus.Outbox.Handler.Config
{
	/// <summary>
    /// Configuration extensions to use transactional outbox for transport
    /// </summary>
    public static class OutboxConfigurationExtensions
    {
        /// <summary>
        /// Adds outbox support for messages handled in a handler
        /// </summary>
        /// <param name="configurer"></param>
        /// <param name="outboxStorageConfigurer"></param>
        /// <returns></returns>
        public static StandardConfigurer<IOutboxTransactionFactory> ForHandlers(
            this StandardConfigurer<IOutboxTransactionFactory> configurer,
            Action<StandardConfigurer<IOutboxStorage>> outboxStorageConfigurer)
        {
            outboxStorageConfigurer(configurer.OtherService<IOutboxStorage>());

            if (outboxStorageConfigurer == null)
                throw new ArgumentNullException(nameof(outboxStorageConfigurer));

            configurer.OtherService<IPipeline>().Decorate(c =>
            {
                var outboxStep = new OutboxStep(c.Get<IOutboxStorage>(), c.Get<ITransport>(), c.Get<IOutboxTransactionFactory>());

                var pipeline = c.Get<IPipeline>();
                return new PipelineStepInjector(pipeline).OnReceive(outboxStep,
                    PipelineRelativePosition.After, typeof(SimpleRetryStrategyStep));
            });

            return configurer;
        }
    }
}
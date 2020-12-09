using System;
using Rebus.Config;
using Rebus.Pipeline;
using Rebus.Pipeline.Receive;
using Rebus.Transport;

namespace Rebus.Outbox
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
        /// <param name="outboxStorageConfigurer"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static RebusConfigurer Outbox(this RebusConfigurer configurer,
            Action<StandardConfigurer<IOutboxStorage>> outboxStorageConfigurer, Action<OutboxOptions> configureOptions = null)
        {
            configurer
                .Transport(t =>
                {
                    outboxStorageConfigurer(t.OtherService<IOutboxStorage>());

                    if (outboxStorageConfigurer == null)
                        throw new ArgumentNullException(nameof(outboxStorageConfigurer));

                    t.Decorate(c =>
                    {
                        var transport = c.Get<ITransport>();
                        return new OutboxTransportDecorator(transport);
                    });
                })
                .Options(opts =>
                {
                    opts.Decorate<IPipeline>(c =>
                    {
                        var outboxOptions = new OutboxOptions();
                        configureOptions?.Invoke(outboxOptions);

                        var outboxStep = outboxOptions.OutboxType == OutboxType.Optimistic ?
                            (IIncomingStep)new OptimisticOutboxStep(c.Get<IOutboxStorage>(), c.Get<ITransport>()) :
                            new PessemisticOutboxStep(c.Get<IOutboxStorage>(), c.Get<ITransport>());

                        var pipeline = c.Get<IPipeline>();
                        return new PipelineStepInjector(pipeline).OnReceive(outboxStep,
                            PipelineRelativePosition.Before, typeof(ActivateHandlersStep));
                    });
                });
            return configurer;
        }
    }
}

using System;
using System.Threading.Tasks;
using Rebus.Config;
using Rebus.Outbox.Common;
using Rebus.Transport;

namespace Rebus.Outbox.Handler.SqlServer.Config
{
    /// <summary>
    /// Configuration extensions for configuring SQL persistence for outbox.
    /// </summary>
    public static class OutboxConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use SQL Server to store outbox messages.
        /// </summary>
        /// <param name="configurer"></param>
        /// <param name="tableName">Outbox messages table name including schema</param>
        /// <param name="automaticallyCreateTables">Create outbox messages table automatically</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static StandardConfigurer<IOutboxStorage> Config(this StandardConfigurer<IOutboxStorage> configurer, string tableName, bool automaticallyCreateTables = true)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));
            if (tableName == null)
                throw new ArgumentNullException(nameof(tableName));
            
            configurer.Register(c =>
            {
                var outboxStorage = new SqlServerOutboxStorage(tableName, c.Get<ITransport>().Address);

                if (automaticallyCreateTables)
                {
                    Task.Run(async () =>
                    {
                        using (var tx = new RebusTransactionScope())
                        {
                            using var outboxTransaction = c.Get<IOutboxTransactionFactory>().Start();
                            await outboxStorage.EnsureTableIsCreated();
                            await outboxTransaction.CompleteAsync();
                            await tx.CompleteAsync();
                        }
                    }).ConfigureAwait(false).GetAwaiter().GetResult();
                }

                return outboxStorage;
            });
            
            return configurer;
        }
    }
}
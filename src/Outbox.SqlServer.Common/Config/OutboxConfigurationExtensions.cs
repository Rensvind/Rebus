using System;
using Microsoft.Data.SqlClient;
using Rebus.Config;
using Rebus.Outbox.Common;

namespace Rebus.Outbox.SqlServer.Common.Config
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
        /// <param name="connectionString">The connection string</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static StandardConfigurer<IOutboxTransactionFactory> UseSqlServer(this StandardConfigurer<IOutboxTransactionFactory> configurer,
            string connectionString)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            configurer.Register(c => new SqlServerOutboxTransactionFactory(connectionString));

            return configurer;
        }

        /// <summary>
        /// Configures Rebus to use SQL Server to store outbox messages.
        /// </summary>
        /// <param name="configurer"></param>
        /// <param name="connectionProvider">The function that returns a SqlConnection</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static StandardConfigurer<IOutboxTransactionFactory> UseSqlServer(this StandardConfigurer<IOutboxTransactionFactory> configurer,
            Func<SqlConnection> connectionProvider)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));
            if (connectionProvider == null)
                throw new ArgumentNullException(nameof(connectionProvider));

            configurer.Register(c => new SqlServerOutboxTransactionFactory(connectionProvider));

            return configurer;
        }
    }
}

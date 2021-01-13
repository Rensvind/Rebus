using System;
using Microsoft.Data.SqlClient;
using Rebus.Config;
using Rebus.Transport;

namespace Rebus.Outbox.SqlServer
{
    /// <summary>
    /// Configuration extensions for configuring SQL persistence for outbox.
    /// </summary>
    public static class SqlServerOutboxStorageConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use SQL Server to store outbox messages.
        /// </summary>
        /// <param name="configurer"></param>
        /// <param name="connectionString">The connection string</param>
        /// <param name="tableName">Outbox messages table name including schema</param>
        /// <param name="automaticallyCreateTables">Create outbox messages table automatically</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void UseSqlServer(this StandardConfigurer<IOutboxStorage> configurer,
            string connectionString, string tableName, bool automaticallyCreateTables = true)
        {
            if (configurer == null)
                throw new ArgumentNullException(nameof(configurer));
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));
            if (tableName == null)
                throw new ArgumentNullException(nameof(tableName));

            configurer.Register(c =>
            {
                var subscriptionStorage = new SqlServerOutboxStorage(new SqlServerOutboxSettings
                {
                    TableName = tableName
                }, c.Get<DbConnectionAccessor>(), c.Get<ITransport>().Address);

                if (automaticallyCreateTables)
                {
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        using (var tx = connection.BeginTransaction())
                        {
                            using (var factory = new DbConnectionFactory(c.Get<DbConnectionAccessor>()))
                            {
                                factory.Set(new DbConnectionAndTransactionWrapper
                                {
                                    DbConnection = connection,
                                    DbTransaction = tx
                                });
                                subscriptionStorage.EnsureTableIsCreated().GetAwaiter().GetResult();
                            }
                            tx.Commit();
                        }

                        connection.Close();
                    }
                }

                return subscriptionStorage;
            });
        }
	}
}
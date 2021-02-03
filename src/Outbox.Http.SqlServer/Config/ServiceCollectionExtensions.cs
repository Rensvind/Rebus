using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Outbox.Common;
using Rebus.Outbox.SqlServer.Common;
using Rebus.Transport;

namespace Rebus.Outbox.Http.SqlServer.Config
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds required services to use http outbox
        /// </summary>
        /// <param name="services">The IServiceCollection</param>
        /// <param name="uniqueApplicationId">An id that uniquely identifies the application</param>
        /// <param name="tableName">The table where to store the http outbox data</param>
        /// <param name="connectionString">The sql server connection string</param>
        /// <param name="automaticallyCreateTable"></param>
        /// <returns></returns>
        public static IServiceCollection AddHttpOutbox(this IServiceCollection services, string uniqueApplicationId, string tableName, string connectionString, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, bool automaticallyCreateTable = true)
        {
            var httpOutboxStorage = new SqlServerHttpOutboxStorage(tableName, uniqueApplicationId);
            var sqlServerOutboxTransactionFactory = new SqlServerOutboxTransactionFactory(connectionString, isolationLevel);

            services.AddSingleton<IOutboxHttpStorage>(httpOutboxStorage);
            services.AddSingleton<IOutboxTransactionFactory>(sqlServerOutboxTransactionFactory);

            if (automaticallyCreateTable)
            {
                using (var scope = new RebusTransactionScope())
                {
                    Task.Run(async () =>
                    {
                        using (var outboxTransaction = sqlServerOutboxTransactionFactory.Start())
                        {
                            await httpOutboxStorage.EnsureTableIsCreated();
                            await outboxTransaction.CompleteAsync();
                        }
                    }).GetAwaiter().GetResult();
                    scope.Complete();
                }
            }


            return services;
        }

        /// <summary>
        /// Adds required services to use http outbox
        /// </summary>
        /// <param name="services">The IServiceCollection</param>
        /// <param name="uniqueApplicationId">An id that uniquely identifies the application</param>
        /// <param name="tableName">The table where to store the http outbox data</param>
        /// <param name="dbConnectionFunc">A function that returns a SqlConnection</param>
        /// <param name="automaticallyCreateTable"></param>
        public static IServiceCollection AddHttpOutbox(this IServiceCollection services, string uniqueApplicationId, string tableName, Func<SqlConnection> dbConnectionFunc, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, bool automaticallyCreateTable = true)
        {
            if (dbConnectionFunc == null)
                throw new ArgumentNullException(nameof(dbConnectionFunc));

            var httpOutboxStorage = new SqlServerHttpOutboxStorage(tableName, uniqueApplicationId);
            var sqlServerOutboxTransactionFactory = new SqlServerOutboxTransactionFactory(dbConnectionFunc, isolationLevel);

            services.AddSingleton<IOutboxHttpStorage>(httpOutboxStorage);
            services.AddSingleton<IOutboxTransactionFactory>(sqlServerOutboxTransactionFactory);

            if (automaticallyCreateTable)
            {
                using (var scope = new RebusTransactionScope())
                {
                    Task.Run(async () =>
                    {
                        using (var outboxTransaction = sqlServerOutboxTransactionFactory.Start())
                        {
                            await httpOutboxStorage.EnsureTableIsCreated();
                            await outboxTransaction.CompleteAsync();
                        }
                    }).GetAwaiter().GetResult();
                    scope.Complete();
                }
            }

            return services;
        }
    }
}

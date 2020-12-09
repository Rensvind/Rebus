using System;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Rebus.Pipeline;

namespace Rebus.Outbox.SqlServer
{
    public class SqlServerOutboxStep : IIncomingStep
    {
        private readonly string connectionString;
        private readonly DbConnectionAccessor connectionAccessor;
        private static int Value;

        public SqlServerOutboxStep(string connectionString, DbConnectionAccessor connectionAccessor)
        {
            this.connectionString = connectionString;
            this.connectionAccessor = connectionAccessor;
        }

        public async Task Process(IncomingStepContext context, Func<Task> next)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var tx = await connection.BeginTransactionAsync())
                {
                    using (var factory = new DbConnectionFactory(connectionAccessor))
                    {
                        factory.Set(new DbConnectionAndTransactionWrapper
                        {
                            DbConnection = connection,
                            DbTransaction = tx
                        });
                        await next();
                    }
                    await tx.CommitAsync();
                }
            }

            // Uncomment to simulate transport failures.
            //if (Value % 2 == 0)
            //{
            //    Value++;
            //    throw new Exception("Try again!");
            //}
        }
    }
}
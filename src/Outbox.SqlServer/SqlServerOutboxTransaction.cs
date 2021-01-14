using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Rebus.Outbox.SqlServer
{
    public class SqlServerOutboxTransaction : IOutboxTransaction
    {
        private readonly SqlConnection connection;
        private readonly SqlTransaction transaction;
        private readonly DbConnectionFactory factory;

        public SqlServerOutboxTransaction(DbConnectionAccessor dbConnectionAccessor, SqlConnection connection, SqlTransaction transaction)
        {
            this.connection = connection;
            this.transaction = transaction;

            this.factory = new DbConnectionFactory(dbConnectionAccessor);
            factory.Set(new DbConnectionAndTransactionWrapper
            {
                DbConnection = connection,
                DbTransaction = transaction
            });
            
        }

        public void Dispose()
        {
            factory.Dispose();
            transaction?.Dispose();
            connection?.Close();
        }

        public Task CompleteAsync()
        {
            return transaction.CommitAsync();
        }
    }
}

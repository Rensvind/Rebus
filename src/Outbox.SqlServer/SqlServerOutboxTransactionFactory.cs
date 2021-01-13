using Microsoft.Data.SqlClient;

namespace Rebus.Outbox.SqlServer
{
    public class SqlServerOutboxTransactionFactory : IOutboxTransactionFactory
    {
        private readonly string connectionString;
        private readonly DbConnectionAccessor dbConnectionAccessor;

        public SqlServerOutboxTransactionFactory(string connectionString, DbConnectionAccessor dbConnectionAccessor)
        {
            this.connectionString = connectionString;
            this.dbConnectionAccessor = dbConnectionAccessor;
        }

        public IOutboxTransaction Start()
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            var tx = connection.BeginTransaction();

            return new SqlServerOutboxTransaction(dbConnectionAccessor, connection, tx);
        }
    }
}
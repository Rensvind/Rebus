using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Rebus.Outbox.Common;

namespace Rebus.Outbox.SqlServer.Common
{
    public class SqlServerOutboxTransactionFactory : IOutboxTransactionFactory
    {
        private readonly Func<SqlConnection> connectionFunc;
        private readonly IsolationLevel isolationLevel;

        public SqlServerOutboxTransactionFactory(string connectionString, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            this.isolationLevel = isolationLevel;

            connectionFunc = () =>
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                return connection;
            };
        }

        public SqlServerOutboxTransactionFactory(Func<SqlConnection> connectionFunc, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            this.connectionFunc = connectionFunc;
            this.isolationLevel = isolationLevel;
        }

        public Rebus.Outbox.Common.OutboxTransaction Start()
        {
            var sqlConnection = connectionFunc();
            
            if(sqlConnection.State != ConnectionState.Open)
                sqlConnection.Open();

            var sqlTransaction = sqlConnection.BeginTransaction(isolationLevel);
            
            return new SqlServerOutboxTransaction(sqlConnection, sqlTransaction);
        }
    }
}
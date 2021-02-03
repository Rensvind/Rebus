using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Rebus.Outbox.SqlServer.Common
{
    public class SqlServerOutboxTransaction : Rebus.Outbox.Common.OutboxTransaction
    {
        public SqlServerOutboxTransaction(SqlConnection connection, SqlTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        public SqlConnection Connection { get; private set; }

        public SqlTransaction Transaction { get; private set; }

        protected override void DisposeInternal()
        {
            Transaction?.Dispose();
            Connection?.Dispose();
            Transaction = null;
            Connection = null;
        }

        public override Task CompleteAsync()
        {
            return Transaction.CommitAsync();
        }
    }
}

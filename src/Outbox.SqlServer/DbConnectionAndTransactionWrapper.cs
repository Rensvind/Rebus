using Microsoft.Data.SqlClient;

namespace Rebus.Outbox.SqlServer
{
    public class DbConnectionAndTransactionWrapper
    {
        public SqlConnection DbConnection { get; set; }
        public SqlTransaction DbTransaction { get; set; }
    }
}
using System.Data.Common;

namespace Rebus.Outbox.SqlServer
{
    public class DbConnectionAndTransactionWrapper
    {
        public DbConnection DbConnection { get; set; }
        public DbTransaction DbTransaction { get; set; }
    }
}
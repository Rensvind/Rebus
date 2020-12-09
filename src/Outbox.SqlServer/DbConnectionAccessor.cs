using System.Threading;

namespace Rebus.Outbox.SqlServer
{
    public class DbConnectionAccessor
    {
        private static readonly AsyncLocal<DbConnectionAndTransactionWrapper> item = new AsyncLocal<DbConnectionAndTransactionWrapper>();

        public DbConnectionAndTransactionWrapper Item
        {
            get => item.Value;
            set => item.Value = value;
        }
    }
}
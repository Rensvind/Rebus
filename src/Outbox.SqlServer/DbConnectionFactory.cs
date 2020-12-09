using System;

namespace Rebus.Outbox.SqlServer
{
    public class DbConnectionFactory : IDisposable
    {
        private readonly DbConnectionAccessor accessor;

        public DbConnectionFactory(DbConnectionAccessor accessor)
        {
            this.accessor = accessor;
        }

        public void Set(DbConnectionAndTransactionWrapper item)
        {
            if (accessor != null)
            {
                accessor.Item = item;
            }
        }

        public void Dispose()
        {
            if (accessor != null)
            {
                accessor.Item = null;
            }
        }
    }
}
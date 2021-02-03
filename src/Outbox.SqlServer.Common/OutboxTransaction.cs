using Rebus.Outbox.Common;
using Rebus.Transport;

namespace Rebus.Outbox.SqlServer.Common
{
    public static class OutboxTransaction
    {
        public static SqlServerOutboxTransaction Get()
        {
            return AmbientTransactionContext.Current.GetOrThrow<SqlServerOutboxTransaction>(OutboxConstants.OutboxTransaction);
        }
    }
}

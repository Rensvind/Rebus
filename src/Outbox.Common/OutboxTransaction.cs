using System;
using System.Threading.Tasks;
using Rebus.Transport;

namespace Rebus.Outbox.Common
{
    public abstract class OutboxTransaction : IDisposable
    {
        protected OutboxTransaction()
        {
            AmbientTransactionContext.Current.Items[OutboxConstants.OutboxTransaction] = this;
        }

        public void Dispose()
        {   
            DisposeInternal();
            AmbientTransactionContext.Current.Items.TryRemove(OutboxConstants.OutboxTransaction, out _);
        }

        public abstract Task CompleteAsync();
        protected abstract void DisposeInternal();
    }
}
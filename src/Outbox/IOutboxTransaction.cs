using System;
using System.Threading.Tasks;

namespace Rebus.Outbox
{
    public interface IOutboxTransaction : IAsyncDisposable
    {
        public Task CompleteAsync();
    }
}
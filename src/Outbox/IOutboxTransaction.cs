using System;
using System.Threading.Tasks;

namespace Rebus.Outbox
{
    public interface IOutboxTransaction : IDisposable
    {
        public Task CompleteAsync();
    }
}
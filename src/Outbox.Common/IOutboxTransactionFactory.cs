namespace Rebus.Outbox.Common
{
    public interface IOutboxTransactionFactory
    {
        public OutboxTransaction Start();
    }
}

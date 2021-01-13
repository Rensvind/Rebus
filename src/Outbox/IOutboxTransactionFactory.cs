namespace Rebus.Outbox
{
    public interface IOutboxTransactionFactory
    {
        public IOutboxTransaction Start();
    }
}

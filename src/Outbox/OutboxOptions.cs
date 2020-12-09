namespace Rebus.Outbox
{
    /// <summary>
    /// Outbox configuration options
    /// </summary>
    public class OutboxOptions
    {
        /// <summary>
        /// Whether to run the outbox optimistic or pessimistic
        /// </summary>
        public OutboxType OutboxType { get; set; } = OutboxType.Optimistic;
    }

    public enum OutboxType
    {
        Optimistic,
        Pessimistic
    }
}

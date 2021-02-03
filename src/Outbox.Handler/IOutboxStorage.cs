using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Outbox.Handler
{
    public interface IOutboxStorage
    {
        /// <summary>
        /// Store the outgoing messages resulting from the incoming message into the outbox
        /// </summary>
        /// <param name="message">The incoming message</param>
        /// <param name="outgoingMessages">The outgoing messages</param>
        /// <returns></returns>
        Task Store(TransportMessage message, IEnumerable<TransportMessage> outgoingMessages);

        /// <summary>
        /// Delete all outgoing messages, except the idempotency (dummy) message, related to supplied incoming message from the outbox
        /// </summary>
        /// <param name="message">The incoming message</param>
        /// <returns></returns>
        Task DeleteOutgoingMessages(TransportMessage message);

        /// <summary>
        /// Delete the idempotency (dummy) message related to the supplied incoming message from the outbox
        /// </summary>
        /// <param name="message">The incoming message</param>
        /// <returns></returns>
        Task DeleteIdempotencyCheckMessage(TransportMessage message);

        /// <summary>
        /// Get outgoing messages for supplied incoming message
        /// </summary>
        /// <param name="message">The incoming message</param>
        /// <returns></returns>
        Task<List<TransportMessage>> GetOutgoingMessages(TransportMessage message);
    }
}
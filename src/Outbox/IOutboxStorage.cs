using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Outbox
{
    public interface IOutboxStorage
    {
        #region Handler functions
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

        #endregion

        #region Non handler functions

        /// <summary>
        /// Store the outgoing messages associated with the provided key
        /// </summary>
        /// <param name="key">The key to associate the messages with</param>
        /// <param name="outgoingMessages">The outgoing messages</param>
        /// <returns></returns>
        Task Store(string key, IEnumerable<TransportMessage> outgoingMessages);

        /// <summary>
        /// Delete all outgoing messages associated with the supplied key from the outbox
        /// </summary>
        /// <param name="key">The key the messages are associated with</param>
        /// <returns></returns>
        Task DeleteOutgoingMessages(string key);

        /// <summary>
        /// Get unsent outgoing messages 
        /// </summary>
        /// <returns></returns>
        Task<List<TransportMessage>> GetUnsentOutgoingMessages(int topMessages);

        #endregion
    }
}
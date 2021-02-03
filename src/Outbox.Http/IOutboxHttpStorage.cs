using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Outbox.Http
{
    public interface IOutboxHttpStorage
    {
        /// <summary>
        /// Store the outgoing messages associated with the provided operation id
        /// </summary>
        /// <param name="operationId">The id to associate the messages with</param>
        /// <param name="outgoingMessages">The outgoing messages</param>
        /// <returns></returns>
        Task Store(string operationId, IEnumerable<TransportMessage> outgoingMessages);

        /// <summary>
        /// Delete all outgoing messages associated with the supplied operation id from the outbox
        /// </summary>
        /// <param name="operationId">The key the messages are associated with</param>
        /// <returns></returns>
        Task DeleteOutgoingMessages(string operationId);

        /// <summary>
        /// Get unsent outgoing messages 
        /// </summary>
        /// <param name="topMessages">Maximum number of messages to send each polling interval</param>
        /// <param name="messagesOlderThan">Only fetch messages older then specified value</param>
        /// <returns></returns>
        Task<List<TransportMessage>> GetUnsentOutgoingMessages(int topMessages, TimeSpan messagesOlderThan);
    }
}

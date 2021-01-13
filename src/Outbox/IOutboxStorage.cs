using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Messages;

namespace Rebus.Outbox
{
    public interface IOutboxStorage
    {
        Task Store(TransportMessage message, IEnumerable<TransportMessage> outgoingMessages);

        Task DeleteOutgoingMessages(TransportMessage message);
        Task DeleteIdempotencyCheckMessage(TransportMessage message);

        Task<List<TransportMessage>> GetOutgoingMessages(TransportMessage message);


        ///// <summary>
        ///// Get outgoing messages for incoming messages
        ///// </summary>
        ///// <param name="message">The incoming message to try to find in the outbox</param>
        ///// <returns>If the message has already been handled it returns a list of <see cref="TransportMessage"/> to send. If result is null it means that the incoming message hasn't been handled before</returns>
        //Task<List<TransportMessage>> GetOutgoingMessages(Message message);

        ///// <summary>
        ///// Try to store the incoming message together with the outgoing messages.
        ///// </summary>
        ///// <param name="message">The incoming message to store in the outbox</param>
        ///// <param name="outgoingMessages">The outgoing messages to store in the outbox together with the incoming message</param>
        ///// <returns>If true then the information has been saved to the outbox. If false then that means the incoming message was already present in the outbox</returns>
        //Task<bool> TryStore(Message message, List<TransportMessage> outgoingMessages);

        Task EnsureTableIsCreated();
    }
}
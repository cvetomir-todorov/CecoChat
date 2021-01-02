using System.Collections.Generic;
using CecoChat.Contracts;

namespace CecoChat.Messaging.Server.Client
{
    public interface IClientContainer
    {
        void AddClient(in int clientID, IStreamingContext<Message> messageStream);

        void RemoveClient(in int clientID, IStreamingContext<Message> messageStream);

        IReadOnlyCollection<IStreamingContext<Message>> GetClients(in int clientID);
    }
}
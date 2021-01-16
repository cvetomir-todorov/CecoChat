using System.Collections.Generic;
using CecoChat.Contracts.Client;

namespace CecoChat.Messaging.Server.Clients
{
    public interface IClientContainer
    {
        void AddClient(in int clientID, IStreamer<ListenResponse> streamer);

        void RemoveClient(in int clientID, IStreamer<ListenResponse> streamer);

        IReadOnlyCollection<IStreamer<ListenResponse>> GetClients(in int clientID);
    }
}
using System.Collections.Generic;
using CecoChat.GrpcContracts;

namespace CecoChat.Messaging.Server.Clients
{
    public interface IClientContainer
    {
        void AddClient(in int clientID, IStreamer<GrpcMessage> streamer);

        void RemoveClient(in int clientID, IStreamer<GrpcMessage> streamer);

        IReadOnlyCollection<IStreamer<GrpcMessage>> GetClients(in int clientID);
    }
}
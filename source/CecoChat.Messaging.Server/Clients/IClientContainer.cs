using System.Collections.Generic;
using CecoChat.GrpcContracts;

namespace CecoChat.Messaging.Server.Clients
{
    public interface IClientContainer
    {
        void AddClient(in int clientID, IStreamingContext<GrpcMessage> messageStream);

        void RemoveClient(in int clientID, IStreamingContext<GrpcMessage> messageStream);

        IReadOnlyCollection<IStreamingContext<GrpcMessage>> GetClients(in int clientID);
    }
}
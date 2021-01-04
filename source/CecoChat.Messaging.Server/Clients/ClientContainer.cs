using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.GrpcContracts;

namespace CecoChat.Messaging.Server.Clients
{
    public sealed class ClientContainer : IClientContainer
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly List<IStreamingContext<GrpcMessage>> _emptyMessageStreamList = new(capacity: 0);
        private readonly ConcurrentDictionary<int, ClientData> _clientsMap;

        public ClientContainer()
        {
            _clientsMap = new ConcurrentDictionary<int, ClientData>();
        }

        public void AddClient(in int clientID, IStreamingContext<GrpcMessage> messageStream)
        {
            ClientData clientData = _clientsMap.GetOrAdd(clientID, _ => new ClientData());
            // TODO: figure out how to avoid same client being added twice
            clientData.MessageStreamList.Add(messageStream);
        }

        public void RemoveClient(in int clientID, IStreamingContext<GrpcMessage> messageStream)
        {
            if (_clientsMap.TryGetValue(clientID, out ClientData clientData))
            {
                clientData.MessageStreamList.Remove(messageStream);
            }
        }

        public IReadOnlyCollection<IStreamingContext<GrpcMessage>> GetClients(in int clientID)
        {
            if (_clientsMap.TryGetValue(clientID, out ClientData clientData))
            {
                return clientData.MessageStreamList;
            }
            else
            {
                return _emptyMessageStreamList;
            }
        }

        private sealed class ClientData
        {
            public List<IStreamingContext<GrpcMessage>> MessageStreamList { get; } = new();
        }
    }
}

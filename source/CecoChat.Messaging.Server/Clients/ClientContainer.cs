using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.Contracts;

namespace CecoChat.Messaging.Server.Clients
{
    public sealed class ClientContainer : IClientContainer
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly List<IStreamingContext<Message>> _emptyMessageStreamList = new List<IStreamingContext<Message>>(capacity: 0);
        private readonly ConcurrentDictionary<int, ClientData> _clientsMap;

        public ClientContainer()
        {
            _clientsMap = new ConcurrentDictionary<int, ClientData>();
        }

        public void AddClient(in int clientID, IStreamingContext<Message> messageStream)
        {
            ClientData clientData = _clientsMap.GetOrAdd(clientID, _ => new ClientData());
            // TODO: figure out how to avoid same client being added twice
            clientData.MessageStreamList.Add(messageStream);
        }

        public void RemoveClient(in int clientID, IStreamingContext<Message> messageStream)
        {
            if (_clientsMap.TryGetValue(clientID, out ClientData clientData))
            {
                clientData.MessageStreamList.Remove(messageStream);
            }
        }

        public IReadOnlyCollection<IStreamingContext<Message>> GetClients(in int clientID)
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
            public List<IStreamingContext<Message>> MessageStreamList { get; } = new List<IStreamingContext<Message>>();
        }
    }
}

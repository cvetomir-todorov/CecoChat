using System.Collections.Concurrent;
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

    public sealed class ClientContainer : IClientContainer
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly List<IStreamer<ListenResponse>> _emptyStreamerList = new(capacity: 0);
        private readonly ConcurrentDictionary<int, ClientData> _clientsMap;

        public ClientContainer()
        {
            _clientsMap = new ConcurrentDictionary<int, ClientData>();
        }

        public void AddClient(in int clientID, IStreamer<ListenResponse> streamer)
        {
            ClientData clientData = _clientsMap.GetOrAdd(clientID, _ => new ClientData());
            // TODO: figure out how to avoid same client being added twice
            clientData.StreamerList.Add(streamer);
        }

        public void RemoveClient(in int clientID, IStreamer<ListenResponse> streamer)
        {
            if (_clientsMap.TryGetValue(clientID, out ClientData clientData))
            {
                clientData.StreamerList.Remove(streamer);
            }
        }

        public IReadOnlyCollection<IStreamer<ListenResponse>> GetClients(in int clientID)
        {
            if (_clientsMap.TryGetValue(clientID, out ClientData clientData))
            {
                return clientData.StreamerList;
            }
            else
            {
                return _emptyStreamerList;
            }
        }

        private sealed class ClientData
        {
            public List<IStreamer<ListenResponse>> StreamerList { get; } = new();
        }
    }
}

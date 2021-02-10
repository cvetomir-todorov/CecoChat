using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.Contracts.Client;

namespace CecoChat.Messaging.Server.Clients
{
    public interface IClientContainer
    {
        bool AddClient(in long userID, IStreamer<ListenResponse> client);

        void RemoveClient(in long userID, IStreamer<ListenResponse> client);

        IEnumerable<IStreamer<ListenResponse>> EnumerateClients(in long userID);

        IEnumerable<KeyValuePair<long, IEnumerable<IStreamer<ListenResponse>>>> EnumerateAllClients();
    }

    public sealed class ClientContainer : IClientContainer
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly List<IStreamer<ListenResponse>> _emptyClientList = new(capacity: 0);
        private readonly ConcurrentDictionary<long, UserClients> _userMap;

        public ClientContainer()
        {
            _userMap = new ConcurrentDictionary<long, UserClients>();
        }

        public bool AddClient(in long userID, IStreamer<ListenResponse> client)
        {
            UserClients userClients = _userMap.GetOrAdd(userID, _ => new UserClients());
            bool isAdded = userClients.Clients.TryAdd(client.ClientID, client);
            return isAdded;
        }

        public void RemoveClient(in long userID, IStreamer<ListenResponse> client)
        {
            if (_userMap.TryGetValue(userID, out UserClients userClients))
            {
                if (!userClients.Clients.TryRemove(client.ClientID, out _))
                {
                    throw new InvalidOperationException($"Client {client.ClientID} for user {userID} has already been removed.");
                }
            }
        }

        public IEnumerable<IStreamer<ListenResponse>> EnumerateClients(in long userID)
        {
            if (_userMap.TryGetValue(userID, out UserClients userClients))
            {
                return userClients;
            }
            else
            {
                return _emptyClientList;
            }
        }

        public IEnumerable<KeyValuePair<long, IEnumerable<IStreamer<ListenResponse>>>> EnumerateAllClients()
        {
            foreach (KeyValuePair<long, UserClients> pair in _userMap)
            {
                long userID = pair.Key;
                IEnumerable<IStreamer<ListenResponse>> clients = pair.Value;

                yield return new KeyValuePair<long, IEnumerable<IStreamer<ListenResponse>>>(userID, clients);
            }
        }

        private sealed class UserClients : IEnumerable<IStreamer<ListenResponse>>
        {
            public ConcurrentDictionary<Guid, IStreamer<ListenResponse>> Clients { get; }

            public UserClients()
            {
                // we don't usually expect the connected clients to be > 1
                Clients = new(concurrencyLevel: Environment.ProcessorCount, capacity: 2);
            }

            public IEnumerator<IStreamer<ListenResponse>> GetEnumerator()
            {
                foreach (var pair in Clients)
                {
                    yield return pair.Value;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}

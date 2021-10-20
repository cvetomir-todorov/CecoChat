using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CecoChat.Contracts.Messaging;

namespace CecoChat.Server.Messaging.Clients
{
    public interface IClientContainer
    {
        bool AddClient(in long userID, IStreamer<ListenNotification> client);

        void RemoveClient(in long userID, IStreamer<ListenNotification> client);

        IEnumerable<IStreamer<ListenNotification>> EnumerateClients(in long userID);

        IEnumerable<KeyValuePair<long, IEnumerable<IStreamer<ListenNotification>>>> EnumerateAllClients();
    }

    public sealed class ClientContainer : IClientContainer
    {
        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly List<IStreamer<ListenNotification>> _emptyClientList = new(capacity: 0);
        private readonly ConcurrentDictionary<long, UserClients> _userMap;

        public ClientContainer()
        {
            _userMap = new ConcurrentDictionary<long, UserClients>();
        }

        public bool AddClient(in long userID, IStreamer<ListenNotification> client)
        {
            UserClients userClients = _userMap.GetOrAdd(userID, _ => new UserClients());
            bool isAdded = userClients.Clients.TryAdd(client.ClientID, client);
            return isAdded;
        }

        public void RemoveClient(in long userID, IStreamer<ListenNotification> client)
        {
            if (_userMap.TryGetValue(userID, out UserClients userClients))
            {
                bool isRemoved = userClients.Clients.TryRemove(client.ClientID, out _);
                if (!isRemoved)
                {
                    throw new InvalidOperationException($"Client {client.ClientID} for user {userID} has already been removed.");
                }
            }
        }

        public IEnumerable<IStreamer<ListenNotification>> EnumerateClients(in long userID)
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

        public IEnumerable<KeyValuePair<long, IEnumerable<IStreamer<ListenNotification>>>> EnumerateAllClients()
        {
            foreach (KeyValuePair<long, UserClients> pair in _userMap)
            {
                long userID = pair.Key;
                IEnumerable<IStreamer<ListenNotification>> clients = pair.Value;

                yield return new KeyValuePair<long, IEnumerable<IStreamer<ListenNotification>>>(userID, clients);
            }
        }

        private sealed class UserClients : IEnumerable<IStreamer<ListenNotification>>
        {
            public ConcurrentDictionary<Guid, IStreamer<ListenNotification>> Clients { get; }

            public UserClients()
            {
                // we don't usually expect the connected clients to be > 1
                Clients = new(concurrencyLevel: Environment.ProcessorCount, capacity: 2);
            }

            public IEnumerator<IStreamer<ListenNotification>> GetEnumerator()
            {
                foreach (KeyValuePair<Guid, IStreamer<ListenNotification>> pair in Clients)
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

using System.Collections;
using System.Collections.Concurrent;
using CecoChat.Contracts.Messaging;

namespace CecoChat.Server.Messaging.Clients.Streaming;

public interface IClientContainer
{
    bool AddClient(in long userId, IStreamer<ListenNotification> client);

    void RemoveClient(in long userId, IStreamer<ListenNotification> client);

    IEnumerable<IStreamer<ListenNotification>> EnumerateClients(in long userId);

    IEnumerable<KeyValuePair<long, IEnumerable<IStreamer<ListenNotification>>>> EnumerateAllClients();
}

public sealed class ClientContainer : IClientContainer
{
    private readonly ConcurrentDictionary<long, UserClients> _userMap;

    public ClientContainer()
    {
        _userMap = new ConcurrentDictionary<long, UserClients>();
    }

    public bool AddClient(in long userId, IStreamer<ListenNotification> client)
    {
        UserClients userClients = _userMap.GetOrAdd(userId, _ => new UserClients());
        bool isAdded = userClients.Clients.TryAdd(client.ClientId, client);
        return isAdded;
    }

    public void RemoveClient(in long userId, IStreamer<ListenNotification> client)
    {
        if (_userMap.TryGetValue(userId, out UserClients? userClients))
        {
            bool isRemoved = userClients.Clients.TryRemove(client.ClientId, out _);
            if (!isRemoved)
            {
                throw new InvalidOperationException($"Client {client.ClientId} for user {userId} has already been removed.");
            }
        }
    }

    public IEnumerable<IStreamer<ListenNotification>> EnumerateClients(in long userId)
    {
        if (_userMap.TryGetValue(userId, out UserClients? userClients))
        {
            return userClients;
        }
        else
        {
            return Array.Empty<IStreamer<ListenNotification>>();
        }
    }

    public IEnumerable<KeyValuePair<long, IEnumerable<IStreamer<ListenNotification>>>> EnumerateAllClients()
    {
        foreach (KeyValuePair<long, UserClients> pair in _userMap)
        {
            long userId = pair.Key;
            IEnumerable<IStreamer<ListenNotification>> clients = pair.Value;

            yield return new KeyValuePair<long, IEnumerable<IStreamer<ListenNotification>>>(userId, clients);
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

using System.Collections.Concurrent;
using System.Globalization;

namespace CecoChat.Server.Messaging.Clients;

public interface IClientContainer
{
    void AddClient(long userId);

    void RemoveClient(long userId);

    IEnumerable<long> EnumerateUsers();

    string GetGroupName(long userId);
}

public class ClientContainer : IClientContainer
{
    private readonly ConcurrentDictionary<long, ClientContext> _clients;

    public ClientContainer()
    {
        // max 64 000 clients per server
        _clients = new(capacity: 8000, concurrencyLevel: Environment.ProcessorCount);
    }

    public void AddClient(long userId)
    {
        ClientContext current = _clients.GetOrAdd(userId, new ClientContext());
        current.IncreaseCount();
    }

    public void RemoveClient(long userId)
    {
        if (!_clients.TryGetValue(userId, out ClientContext? current))
        {
            throw new InvalidOperationException("When decreasing count of clients for a specific user, it should had been already added.");
        }

        current.DecreaseCount();
    }

    public IEnumerable<long> EnumerateUsers()
    {
        foreach (KeyValuePair<long,ClientContext> pair in _clients)
        {
            long userId = pair.Key;
            ClientContext context = pair.Value;

            if (context.Count > 0)
            {
                yield return userId;
            }
        }
    }

    public string GetGroupName(long userId)
    {
        return userId.ToString(CultureInfo.InvariantCulture);
    }

    private sealed class ClientContext
    {
        private int _count;

        public int Count => _count;

        public void IncreaseCount()
        {
            Interlocked.Increment(ref _count);
        }

        public void DecreaseCount()
        {
            Interlocked.Decrement(ref _count);
        }
    }
}

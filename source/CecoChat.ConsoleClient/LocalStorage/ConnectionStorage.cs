using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace CecoChat.ConsoleClient.LocalStorage;

public class ConnectionStorage
{
    private readonly ConcurrentDictionary<long, Connection> _connectionsMap;

    public ConnectionStorage()
    {
        _connectionsMap = new();
    }

    public IEnumerable<Connection> EnumerateConnections()
    {
        foreach (KeyValuePair<long, Connection> pair in _connectionsMap)
        {
            yield return pair.Value;
        }
    }

    public void ReplaceConnections(IEnumerable<Connection> connections)
    {
        _connectionsMap.Clear();

        foreach (Connection connection in connections)
        {
            _connectionsMap.TryAdd(connection.ConnectionId, connection);
        }
    }

    public Connection? GetConnection(long userId)
    {
        _connectionsMap.TryGetValue(userId, out Connection? connection);
        return connection;
    }
}

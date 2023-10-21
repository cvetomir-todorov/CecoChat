using System.Collections.Concurrent;

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

    public void AddConnection(long userId, ConnectionStatus status, Guid version)
    {
        Connection connection = new()
        {
            ConnectionId = userId,
            Status = status,
            Version = version
        };

        _connectionsMap.TryAdd(connection.ConnectionId, connection);
    }

    public void UpdateConnection(long userId, ConnectionStatus newStatus, Guid newVersion)
    {
        if (!_connectionsMap.TryGetValue(userId, out Connection? connection))
        {
            throw new InvalidOperationException($"Failed to get an existing connection for user {userId}.");
        }

        connection.Status = newStatus;
        connection.Version = newVersion;
    }

    public void AddOrUpdateConnection(Connection connection)
    {
        if (_connectionsMap.TryGetValue(connection.ConnectionId, out Connection? existing))
        {
            existing.Status = connection.Status;
            existing.Version = connection.Version;
        }
        else
        {
            _connectionsMap.TryAdd(connection.ConnectionId, connection);
        }
    }

    public void RemoveConnection(long connectionId)
    {
        _connectionsMap.TryRemove(connectionId, out _);
    }
}

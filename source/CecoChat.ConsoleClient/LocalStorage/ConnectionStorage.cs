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

    public Connection? GetConnection(long connectionId)
    {
        _connectionsMap.TryGetValue(connectionId, out Connection? connection);
        return connection;
    }

    public void UpdateConnections(IEnumerable<Connection> connections)
    {
        foreach (Connection connection in connections)
        {
            UpdateConnection(connection);
        }
    }

    public void UpdateConnection(Connection connection)
    {
        UpdateConnection(connection.ConnectionId, connection.Status, connection.Version);
    }

    public void UpdateConnection(long connectionId, ConnectionStatus newStatus, DateTime newVersion)
    {
        _connectionsMap.AddOrUpdate(
            connectionId,
            addValueFactory: id =>
            {
                return new Connection
                {
                    ConnectionId = id, Status = newStatus, Version = newVersion
                };
            },
            updateValueFactory: (id, existing) =>
            {
                if (existing.Version >= newVersion)
                {
                    return existing;
                }

                return new Connection
                {
                    ConnectionId = id, Status = newStatus, Version = newVersion
                };
            });
    }
}

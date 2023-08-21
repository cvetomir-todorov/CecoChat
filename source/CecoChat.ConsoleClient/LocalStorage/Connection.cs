namespace CecoChat.ConsoleClient.LocalStorage;

public sealed class Connection
{
    public long ConnectionId { get; init; }
    public Guid Version { get; init; }
    public ConnectionStatus Status { get; init; }
}

public enum ConnectionStatus
{
    Pending,
    Connected
}

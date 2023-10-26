namespace CecoChat.ConsoleClient.LocalStorage;

public sealed class Connection
{
    public long ConnectionId { get; init; }
    public DateTime Version { get; set; }
    public ConnectionStatus Status { get; set; }
}

public enum ConnectionStatus
{
    NotConnected,
    Pending,
    Connected
}

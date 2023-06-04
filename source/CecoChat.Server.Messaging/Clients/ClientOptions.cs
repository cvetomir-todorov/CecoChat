namespace CecoChat.Server.Messaging.Clients;

public sealed class ClientOptions
{
    public TimeSpan TimeoutInterval { get; init; }

    public TimeSpan KeepAliveInterval { get; init; }
}

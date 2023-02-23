namespace CecoChat.Server.Messaging.Clients;

public sealed class ClientOptions
{
    public TimeSpan TimeoutInterval { get; set; }

    public TimeSpan KeepAliveInterval { get; set; }
}

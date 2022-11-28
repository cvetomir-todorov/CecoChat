using CecoChat.Http.Client;
using CecoChat.Polly;

namespace CecoChat.Client.State;

public sealed class StateOptions
{
    public Uri? Address { get; set; }
    public TimeSpan CallTimeout { get; set; }

    public SocketsHttpHandlerOptions? SocketsHttpHandler { get; set; }
    public RetryOptions? Retry { get; set; }
}
using Common.Http.Client;
using Common.Polly;

namespace CecoChat.User.Client;

public sealed class UserClientOptions
{
    public Uri? Address { get; init; }
    public TimeSpan CallTimeout { get; init; }

    public SocketsHttpHandlerOptions? SocketsHttpHandler { get; init; }
    public RetryOptions? Retry { get; init; }

    public string HealthPath { get; init; } = string.Empty;
    public TimeSpan HealthTimeout { get; init; }
}

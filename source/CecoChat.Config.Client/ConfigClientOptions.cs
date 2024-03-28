using Common.Http.Client;
using Common.Polly;

namespace CecoChat.Config.Client;

public sealed class ConfigClientOptions
{
    public Uri? Address { get; init; }
    public TimeSpan CallTimeout { get; init; }

    public SocketsHttpHandlerOptions? SocketsHttpHandler { get; init; }
    public RetryOptions? Retry { get; init; }

    public string HealthPath { get; init; } = string.Empty;
    public TimeSpan HealthTimeout { get; init; }
}

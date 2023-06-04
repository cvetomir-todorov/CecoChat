using CecoChat.Http.Client;
using CecoChat.Polly;

namespace CecoChat.Client.History;

public sealed class HistoryOptions
{
    public Uri? Address { get; init; }
    public TimeSpan CallTimeout { get; init; }

    public SocketsHttpHandlerOptions? SocketsHttpHandler { get; init; }
    public RetryOptions? Retry { get; init; }

    public string HealthPath { get; init; } = string.Empty;
    public TimeSpan HealthTimeout { get; init; }
}

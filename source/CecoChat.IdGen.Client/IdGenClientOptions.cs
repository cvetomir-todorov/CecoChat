using Common.Http.Client;
using Common.Polly;

namespace CecoChat.IdGen.Client;

public sealed class IdGenClientOptions
{
    public Uri? Address { get; init; }
    public TimeSpan CallTimeout { get; init; }
    public int RefreshIdsCount { get; init; }
    public TimeSpan RefreshIdsInterval { get; init; }
    public TimeSpan GetIdWaitInterval { get; init; }

    public SocketsHttpHandlerOptions? SocketsHttpHandler { get; init; }
    public RetryOptions? Retry { get; init; }

    public string HealthPath { get; init; } = string.Empty;
    public TimeSpan HealthTimeout { get; init; }
}

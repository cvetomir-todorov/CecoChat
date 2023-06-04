using CecoChat.Http.Client;
using CecoChat.Polly;

namespace CecoChat.Client.IdGen;

public sealed class IdGenOptions
{
    public Uri? Address { get; init; }
    public TimeSpan CallTimeout { get; init; }
    public long OriginatorId { get; init; }
    public int RefreshIdsCount { get; init; }
    public TimeSpan RefreshIdsInterval { get; init; }
    public TimeSpan GetIdWaitInterval { get; init; }

    public SocketsHttpHandlerOptions? SocketsHttpHandler { get; init; }
    public RetryOptions? Retry { get; init; }

    public string HealthPath { get; init; } = string.Empty;
    public TimeSpan HealthTimeout { get; init; }
}

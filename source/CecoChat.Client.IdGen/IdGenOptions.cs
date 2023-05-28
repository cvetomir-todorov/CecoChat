using CecoChat.Http.Client;
using CecoChat.Polly;

namespace CecoChat.Client.IdGen;

public sealed class IdGenOptions
{
    public Uri? Address { get; set; }
    public TimeSpan CallTimeout { get; set; }
    public long OriginatorId { get; set; }
    public int RefreshIdsCount { get; set; }
    public TimeSpan RefreshIdsInterval { get; set; }
    public TimeSpan GetIdWaitInterval { get; set; }

    public SocketsHttpHandlerOptions? SocketsHttpHandler { get; set; }
    public RetryOptions? Retry { get; set; }

    public string HealthPath { get; set; } = string.Empty;
    public TimeSpan HealthTimeout { get; set; }
}

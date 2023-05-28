using CecoChat.Http.Client;
using CecoChat.Polly;

namespace CecoChat.Client.IdGen;

public sealed class IDGenOptions
{
    public Uri? Address { get; set; }
    public TimeSpan CallTimeout { get; set; }
    public long OriginatorID { get; set; }
    public int RefreshIDsCount { get; set; }
    public TimeSpan RefreshIDsInterval { get; set; }
    public TimeSpan GetIDWaitInterval { get; set; }

    public SocketsHttpHandlerOptions? SocketsHttpHandler { get; set; }
    public RetryOptions? Retry { get; set; }

    public string HealthPath { get; set; } = string.Empty;
    public TimeSpan HealthTimeout { get; set; }
}

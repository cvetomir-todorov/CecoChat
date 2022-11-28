using CecoChat.Http.Client;
using CecoChat.Polly;

namespace CecoChat.Client.History;

public sealed class HistoryOptions
{
    public Uri? Address { get; set; }
    public TimeSpan CallTimeout { get; set; }

    public SocketsHttpHandlerOptions? SocketsHttpHandler { get; set; }
    public RetryOptions? Retry { get; set; }

    public string HealthPath { get; set; } = string.Empty;
    public TimeSpan HealthTimeout { get; set; }
}

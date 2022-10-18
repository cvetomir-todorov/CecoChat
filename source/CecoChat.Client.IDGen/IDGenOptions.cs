using System;
using CecoChat.HttpClient;
using CecoChat.Polly;

namespace CecoChat.Client.IDGen;

public sealed class IDGenOptions
{
    public Uri Address { get; set; }
    public TimeSpan CallTimeout { get; set; }
    public long OriginatorID { get; set; }
    public int RefreshIDsCount { get; set; }
    public TimeSpan RefreshIDsInterval { get; set; }
    public TimeSpan GetIDWaitInterval { get; set; }

    public SocketsHttpHandlerOptions SocketsHttpHandler { get; set; }
    public RetryOptions Retry { get; set; }
}
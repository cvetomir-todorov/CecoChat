namespace Common.Http.Client;

public sealed class SocketsHttpHandlerOptions
{
    public TimeSpan KeepAlivePingDelay { get; init; }
    public TimeSpan KeepAlivePingTimeout { get; init; }
    public bool EnableMultipleHttp2Connections { get; init; }
}

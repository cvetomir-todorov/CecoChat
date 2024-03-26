using Microsoft.Extensions.DependencyInjection;

namespace Common.Http.Client;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder ConfigureSocketsPrimaryHttpClientHandler(this IHttpClientBuilder builder, SocketsHttpHandlerOptions options)
    {
        return builder.ConfigurePrimaryHttpMessageHandler(() => CreateMessageHandler(options));
    }

    private static HttpMessageHandler CreateMessageHandler(SocketsHttpHandlerOptions options)
    {
        return new SocketsHttpHandler
        {
            KeepAlivePingDelay = options.KeepAlivePingDelay,
            KeepAlivePingTimeout = options.KeepAlivePingTimeout,
            EnableMultipleHttp2Connections = options.EnableMultipleHttp2Connections,
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan
        };
    }
}

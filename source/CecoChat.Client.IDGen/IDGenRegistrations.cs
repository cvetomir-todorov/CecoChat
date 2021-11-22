using System.Net.Http;
using System.Threading;
using CecoChat.Polly;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.IDGen
{
    public static class IDGenRegistrations
    {
        public static void AddIDGenClient(this IServiceCollection services, IDGenOptions options)
        {
            services
                .AddGrpcClient<Contracts.IDGen.IDGen.IDGenClient>(grpc =>
                {
                    grpc.Address = options.Communication.Address;
                })
                .ConfigurePrimaryHttpMessageHandler(() => CreateMessageHandler(options))
                .AddGrpcRetryPolicy(options.Retry);
        }

        private static HttpMessageHandler CreateMessageHandler(IDGenOptions options)
        {
            return new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = options.Communication.KeepAlivePingDelay,
                KeepAlivePingTimeout = options.Communication.KeepAlivePingTimeout,
                EnableMultipleHttp2Connections = true
            };
        }
    }
}

using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.History
{
    public static class HistoryRegistrations
    {
        public static void AddHistoryClient(this IServiceCollection services, HistoryOptions options)
        {
            services.AddGrpcClient<Contracts.History.History.HistoryClient>(grpc =>
                {
                    grpc.Address = options.Address;
                })
                .ConfigurePrimaryHttpMessageHandler(CreateMessageHandler);
        }

        private static HttpMessageHandler CreateMessageHandler()
        {
            return new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                EnableMultipleHttp2Connections = true
            };
        }
    }
}
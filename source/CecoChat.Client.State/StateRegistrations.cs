using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.State
{
    public static class StateRegistrations
    {
        public static void AddStateClient(this IServiceCollection services, StateOptions options)
        {
            services.AddGrpcClient<Contracts.State.State.StateClient>(grpc =>
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
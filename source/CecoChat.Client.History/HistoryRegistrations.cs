using CecoChat.HttpClient;
using CecoChat.Polly;
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
                .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
                .AddGrpcRetryPolicy(options.Retry);
        }
    }
}
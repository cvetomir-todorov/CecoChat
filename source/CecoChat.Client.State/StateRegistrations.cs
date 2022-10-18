using CecoChat.HttpClient;
using CecoChat.Polly;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.State;

public static class StateRegistrations
{
    public static void AddStateClient(this IServiceCollection services, StateOptions options)
    {
        services.AddGrpcClient<Contracts.State.State.StateClient>(grpc =>
            {
                grpc.Address = options.Address;
            })
            .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);
    }
}
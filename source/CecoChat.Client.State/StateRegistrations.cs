using CecoChat.HttpClient;
using CecoChat.Polly;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.State;

public static class StateRegistrations
{
    public static void AddStateClient(this IServiceCollection services, StateOptions options)
    {
        if (options.SocketsHttpHandler == null)
        {
            throw new ArgumentNullException(nameof(options.SocketsHttpHandler));
        }
        if (options.Retry == null)
        {
            throw new ArgumentNullException(nameof(options.Retry));
        }

        services.AddGrpcClient<Contracts.State.State.StateClient>(grpc =>
            {
                grpc.Address = options.Address;
            })
            .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);
    }
}
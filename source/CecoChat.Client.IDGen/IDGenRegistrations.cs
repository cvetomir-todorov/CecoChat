using CecoChat.HttpClient;
using CecoChat.Polly;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.IDGen;

public static class IDGenRegistrations
{
    public static void AddIDGenClient(this IServiceCollection services, IDGenOptions options)
    {
        if (options.SocketsHttpHandler == null)
        {
            throw new ArgumentNullException(nameof(options), $"{nameof(options.SocketsHttpHandler)}");
        }
        if (options.Retry == null)
        {
            throw new ArgumentNullException(nameof(options), $"{nameof(options.Retry)}");
        }

        services
            .AddGrpcClient<Contracts.IDGen.IDGen.IDGenClient>(grpc =>
            {
                grpc.Address = options.Address;
            })
            .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);
    }
}
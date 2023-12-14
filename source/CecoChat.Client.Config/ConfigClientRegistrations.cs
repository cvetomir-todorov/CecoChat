using CecoChat.Http.Client;
using CecoChat.Polly;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.Config;

public static class ConfigClientRegistrations
{
    public static void AddConfigClient(this IServiceCollection services, ConfigClientOptions options)
    {
        if (options.SocketsHttpHandler == null)
        {
            throw new ArgumentNullException(nameof(options), $"{nameof(options.SocketsHttpHandler)}");
        }
        if (options.Retry == null)
        {
            throw new ArgumentNullException(nameof(options), $"{nameof(options.Retry)}");
        }

        services.AddGrpcClient<Contracts.Config.Config.ConfigClient>(grpc =>
            {
                grpc.Address = options.Address;
            })
            .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);
    }
}

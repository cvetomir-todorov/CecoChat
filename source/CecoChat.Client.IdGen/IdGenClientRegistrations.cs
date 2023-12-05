using CecoChat.Http.Client;
using CecoChat.Polly;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.IdGen;

public static class IdGenClientRegistrations
{
    public static void AddIdGenClient(this IServiceCollection services, IdGenClientOptions options)
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
            .AddGrpcClient<Contracts.IdGen.IdGen.IdGenClient>(grpc =>
            {
                grpc.Address = options.Address;
            })
            .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);
    }
}

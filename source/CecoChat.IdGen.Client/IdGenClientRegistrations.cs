using Common.Http.Client;
using Common.Polly;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.IdGen.Client;

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
            .AddGrpcClient<CecoChat.IdGen.Contracts.IdGen.IdGenClient>(grpc =>
            {
                grpc.Address = options.Address;
            })
            .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);
    }
}

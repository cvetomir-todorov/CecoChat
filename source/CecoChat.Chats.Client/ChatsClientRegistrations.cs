using Common.Http.Client;
using Common.Polly;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Chats.Client;

public static class ChatsClientRegistrations
{
    public static void AddChatsClient(this IServiceCollection services, ChatsClientOptions options)
    {
        if (options.SocketsHttpHandler == null)
        {
            throw new ArgumentNullException(nameof(options), $"{nameof(options.SocketsHttpHandler)}");
        }
        if (options.Retry == null)
        {
            throw new ArgumentNullException(nameof(options), $"{nameof(options.Retry)}");
        }

        services.AddGrpcClient<CecoChat.Chats.Contracts.Chats.ChatsClient>(grpc =>
            {
                grpc.Address = options.Address;
            })
            .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);
    }
}

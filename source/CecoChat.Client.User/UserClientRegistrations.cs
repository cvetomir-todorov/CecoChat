using CecoChat.User.Contracts;
using Common.Http.Client;
using Common.Polly;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Client.User;

public static class UserClientRegistrations
{
    public static void AddUserClient(this IServiceCollection services, UserClientOptions options)
    {
        if (options.SocketsHttpHandler == null)
        {
            throw new ArgumentNullException(nameof(options), $"{nameof(options.SocketsHttpHandler)}");
        }
        if (options.Retry == null)
        {
            throw new ArgumentNullException(nameof(options), $"{nameof(options.Retry)}");
        }

        services.AddGrpcClient<Auth.AuthClient>(grpc =>
            {
                grpc.Address = options.Address;
            })
            .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);

        services.AddGrpcClient<ProfileCommand.ProfileCommandClient>(grpc =>
            {
                grpc.Address = options.Address;
            })
            .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);

        services.AddGrpcClient<ProfileQuery.ProfileQueryClient>(grpc =>
            {
                grpc.Address = options.Address;
            })
            .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);

        services.AddGrpcClient<ConnectionCommand.ConnectionCommandClient>(grpc =>
            {
                grpc.Address = options.Address;
            })
            .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);

        services.AddGrpcClient<ConnectionQuery.ConnectionQueryClient>(grpc =>
            {
                grpc.Address = options.Address;
            })
            .ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);

        services.AddGrpcClient<FileCommand.FileCommandClient>(grpc =>
            {
                grpc.Address = options.Address;
            }).ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);

        services.AddGrpcClient<FileQuery.FileQueryClient>(grpc =>
            {
                grpc.Address = options.Address;
            }).ConfigureSocketsPrimaryHttpClientHandler(options.SocketsHttpHandler)
            .AddGrpcRetryPolicy(options.Retry);
    }
}

using CecoChat.Grpc.Instrumentation;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Grpc
{
    public static class GrpcExtensions
    {
        public static IServiceCollection AddGrpcCustomUtilies(this IServiceCollection services)
        {
            return services
                .AddSingleton<IGrpcActivityUtility, GrpcActivityUtility>();
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Redis
{
    public static class RedisExtensions
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration redisConfiguration)
        {
            return services
                .AddSingleton<IRedisContext, RedisContext>()
                .Configure<RedisOptions>(redisConfiguration);
        }
    }
}

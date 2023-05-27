using Autofac;
using CecoChat.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Redis;

public sealed class RedisAutofacModule : Module
{
    private readonly IConfiguration _redisConfiguration;

    public RedisAutofacModule(IConfiguration redisConfiguration)
    {
        _redisConfiguration = redisConfiguration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<RedisContext>().As<IRedisContext>().SingleInstance();
        builder.RegisterOptions<RedisOptions>(_redisConfiguration);
    }
}

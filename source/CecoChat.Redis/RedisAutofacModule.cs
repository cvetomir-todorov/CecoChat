using Autofac;
using CecoChat.Autofac;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Redis;

public sealed class RedisAutofacModule : Module
{
    private readonly IConfiguration _redisConfiguration;
    private readonly string? _name;

    public RedisAutofacModule(IConfiguration redisConfiguration, string? name = null)
    {
        _redisConfiguration = redisConfiguration;
        _name = name;
    }

    protected override void Load(ContainerBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            builder
                .RegisterType<RedisContext>()
                .As<IRedisContext>()
                .SingleInstance();
        }
        else
        {
            builder
                .RegisterType<RedisContext>()
                .As<IRedisContext>()
                .Named<IRedisContext>(_name)
                .SingleInstance();
        }

        builder.RegisterOptions<RedisOptions>(_redisConfiguration);
    }
}

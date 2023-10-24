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
                .Register(_ => _redisConfiguration.Get<RedisOptions>()!)
                .As<RedisOptions>()
                .SingleInstance();
            builder
                .RegisterType<RedisContext>()
                .As<IRedisContext>()
                .SingleInstance();
        }
        else
        {
            string optionsName = $"{_name}-options";

            builder
                .Register(_ => _redisConfiguration.Get<RedisOptions>()!)
                .Named<RedisOptions>(optionsName)
                .SingleInstance();
            builder
                .RegisterType<RedisContext>()
                .Named<IRedisContext>(_name)
                .WithNamedParameter(typeof(RedisOptions), parameterName: optionsName)
                .SingleInstance();
        }
    }
}

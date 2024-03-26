using Autofac;
using Common.Autofac;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Common.Redis;

public sealed class RedisAutofacModule : Module
{
    private readonly IConfiguration _redisConfiguration;
    private readonly string? _name;
    private readonly bool _registerConnectionMultiplexer;

    public RedisAutofacModule(IConfiguration redisConfiguration, string? name = null, bool registerConnectionMultiplexer = false)
    {
        _redisConfiguration = redisConfiguration;
        _name = name;
        _registerConnectionMultiplexer = registerConnectionMultiplexer;
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

            if (_registerConnectionMultiplexer)
            {
                builder
                    .Register(container =>
                    {
                        IRedisContext redisContext = container.Resolve<IRedisContext>();
                        return redisContext.Connection;
                    })
                    .As<IConnectionMultiplexer>()
                    .SingleInstance();
            }
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

            if (_registerConnectionMultiplexer)
            {
                builder
                    .Register(container =>
                    {
                        IRedisContext redisContext = container.ResolveNamed<IRedisContext>(_name);
                        return redisContext.Connection;
                    })
                    .As<IConnectionMultiplexer>()
                    .SingleInstance();
            }
        }
    }
}

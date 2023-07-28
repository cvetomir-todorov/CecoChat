using Autofac;
using CecoChat.Autofac;
using CecoChat.Data.Config.History;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Data.Config.Snowflake;
using CecoChat.Events;
using CecoChat.Redis;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.Config;

public sealed class ConfigDbAutofacModule : Module
{
    private readonly IConfiguration _redisConfiguration;
    private readonly bool _registerHistory;
    private readonly bool _registerPartitioning;
    private readonly bool _registerSnowflake;

    public ConfigDbAutofacModule(IConfiguration redisConfiguration, bool registerHistory = false, bool registerPartitioning = false, bool registerSnowflake = false)
    {
        _redisConfiguration = redisConfiguration;
        _registerHistory = registerHistory;
        _registerPartitioning = registerPartitioning;
        _registerSnowflake = registerSnowflake;
    }

    public static readonly string RedisContextName = "config";

    protected override void Load(ContainerBuilder builder)
    {
        if (_registerHistory || _registerPartitioning || _registerSnowflake)
        {
            builder.RegisterModule(new RedisAutofacModule(_redisConfiguration, RedisContextName));
            builder.RegisterType<ConfigUtility>().As<IConfigUtility>().SingleInstance();
        }
        if (_registerHistory)
        {
            builder
                .RegisterType<HistoryConfig>()
                .As<IHistoryConfig>()
                .WithNamedParameter(typeof(IRedisContext), RedisContextName)
                .SingleInstance();
            builder
                .RegisterType<HistoryConfigRepo>()
                .As<IHistoryConfigRepo>()
                .WithNamedParameter(typeof(IRedisContext), RedisContextName)
                .SingleInstance();
        }
        if (_registerPartitioning)
        {
            builder
                .RegisterType<PartitioningConfig>()
                .As<IPartitioningConfig>()
                .WithNamedParameter(typeof(IRedisContext), RedisContextName)
                .SingleInstance();
            builder
                .RegisterType<PartitioningConfigRepo>()
                .As<IPartitioningConfigRepo>()
                .WithNamedParameter(typeof(IRedisContext), RedisContextName)
                .SingleInstance();
            builder.RegisterSingletonEvent<EventSource<PartitionsChangedEventData>, PartitionsChangedEventData>();
        }
        if (_registerSnowflake)
        {
            builder
                .RegisterType<SnowflakeConfig>()
                .As<ISnowflakeConfig>()
                .WithNamedParameter(typeof(IRedisContext), RedisContextName)
                .SingleInstance();
            builder
                .RegisterType<SnowflakeConfigRepo>()
                .As<ISnowflakeConfigRepo>()
                .WithNamedParameter(typeof(IRedisContext), RedisContextName)
                .SingleInstance();
        }
    }
}

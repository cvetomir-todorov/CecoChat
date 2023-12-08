using Autofac;
using CecoChat.Autofac;
using CecoChat.Data.Config.History;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Data.Config.Snowflake;
using CecoChat.Events;
using CecoChat.Npgsql;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.Config;

public sealed class ConfigDbAutofacModule : Module
{
    private readonly IConfiguration _configDbConfiguration;
    private readonly bool _registerHistory;
    private readonly bool _registerPartitioning;
    private readonly bool _registerSnowflake;

    public ConfigDbAutofacModule(IConfiguration configDbConfiguration, bool registerHistory = false, bool registerPartitioning = false, bool registerSnowflake = false)
    {
        _configDbConfiguration = configDbConfiguration;
        _registerHistory = registerHistory;
        _registerPartitioning = registerPartitioning;
        _registerSnowflake = registerSnowflake;
    }

    protected override void Load(ContainerBuilder builder)
    {
        if (_registerHistory || _registerPartitioning || _registerSnowflake)
        {
            builder.RegisterType<NpgsqlDbInitializer>().As<INpgsqlDbInitializer>().SingleInstance();
            builder.RegisterType<ConfigUtility>().As<IConfigUtility>().SingleInstance();
            builder.RegisterOptions<ConfigDbOptions>(_configDbConfiguration);
        }
        if (_registerHistory)
        {
            builder
                .RegisterType<HistoryConfig>()
                .As<IHistoryConfig>()
                .SingleInstance();
            builder
                .RegisterType<HistoryRepo>()
                .As<IHistoryRepo>()
                .SingleInstance();
        }
        if (_registerPartitioning)
        {
            builder
                .RegisterType<PartitioningConfig>()
                .As<IPartitioningConfig>()
                .SingleInstance();
            builder
                .RegisterType<PartitioningRepo>()
                .As<IPartitioningRepo>()
                .SingleInstance();
            builder.RegisterSingletonEvent<EventSource<EventArgs>, EventArgs>();
        }
        if (_registerSnowflake)
        {
            builder
                .RegisterType<SnowflakeConfig>()
                .As<ISnowflakeConfig>()
                .SingleInstance();
            builder
                .RegisterType<SnowflakeRepo>()
                .As<ISnowflakeRepo>()
                .SingleInstance();
        }
    }
}

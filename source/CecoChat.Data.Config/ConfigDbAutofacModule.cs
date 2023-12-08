using Autofac;
using CecoChat.Autofac;
using CecoChat.Data.Config.Common;
using CecoChat.Data.Config.History;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Data.Config.Snowflake;
using CecoChat.Events;
using CecoChat.Npgsql;
using FluentValidation;
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
            builder.RegisterOptions<ConfigDbOptions>(_configDbConfiguration);
        }
        if (_registerHistory)
        {
            builder.RegisterType<HistoryConfig>().As<IHistoryConfig>().SingleInstance();
            builder.RegisterType<ConfigSection<HistoryValues>>().As<IConfigSection<HistoryValues>>().SingleInstance();
            builder.RegisterType<HistoryRepo>().As<IRepo<HistoryValues>>().SingleInstance();
            builder.RegisterType<HistoryValidator>().As<IValidator<HistoryValues>>().SingleInstance();
        }
        if (_registerPartitioning)
        {
            builder.RegisterType<PartitioningConfig>().As<IPartitioningConfig>().SingleInstance();
            builder.RegisterType<ConfigSection<PartitioningValues>>().As<IConfigSection<PartitioningValues>>().SingleInstance();
            builder.RegisterType<PartitioningRepo>().As<IRepo<PartitioningValues>>().SingleInstance();
            builder.RegisterType<PartitioningValidator>().As<IValidator<PartitioningValues>>().SingleInstance();
            builder.RegisterSingletonEvent<EventSource<EventArgs>, EventArgs>();
        }
        if (_registerSnowflake)
        {
            builder.RegisterType<SnowflakeConfig>().As<ISnowflakeConfig>().SingleInstance();
            builder.RegisterType<ConfigSection<SnowflakeValues>>().As<IConfigSection<SnowflakeValues>>().SingleInstance();
            builder.RegisterType<SnowflakeRepo>().As<IRepo<SnowflakeValues>>().SingleInstance();
            builder.RegisterType<SnowflakeValidator>().As<IValidator<SnowflakeValues>>().SingleInstance();
        }
    }
}

using Autofac;
using CecoChat.Config.Contracts;
using CecoChat.DynamicConfig.Backplane;
using CecoChat.DynamicConfig.Sections.History;
using CecoChat.DynamicConfig.Sections.Partitioning;
using CecoChat.DynamicConfig.Sections.Snowflake;
using CecoChat.DynamicConfig.Sections.User;
using Common.Autofac;
using Common.Events;
using Common.Kafka;
using Common.Kafka.Telemetry;
using Confluent.Kafka;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace CecoChat.DynamicConfig;

public sealed class DynamicConfigAutofacModule : Module
{
    private readonly IConfiguration? _backplaneConfiguration;
    private readonly bool _registerConfigChangesProducer;
    private readonly bool _registerConfigChangesConsumer;
    private readonly bool _registerHistory;
    private readonly bool _registerPartitioning;
    private readonly bool _registerSnowflake;
    private readonly bool _registerUser;

    public DynamicConfigAutofacModule(
        IConfiguration? backplaneConfiguration = null,
        bool registerConfigChangesProducer = false,
        bool registerConfigChangesConsumer = false,
        bool registerHistory = false,
        bool registerPartitioning = false,
        bool registerSnowflake = false,
        bool registerUser = false)
    {
        _backplaneConfiguration = backplaneConfiguration;

        _registerConfigChangesProducer = registerConfigChangesProducer;
        _registerConfigChangesConsumer = registerConfigChangesConsumer;

        _registerHistory = registerHistory;
        _registerPartitioning = registerPartitioning;
        _registerSnowflake = registerSnowflake;
        _registerUser = registerUser;
    }

    protected override void Load(ContainerBuilder builder)
    {
        if (_registerConfigChangesProducer)
        {
            if (_backplaneConfiguration == null)
            {
                throw new InvalidOperationException("Backplane configuration should not be null.");
            }

            builder.RegisterType<ConfigChangesProducer>().As<IConfigChangesProducer>().SingleInstance();
            builder.RegisterFactory<KafkaProducer<Null, ConfigChange>, IKafkaProducer<Null, ConfigChange>>();
            builder.RegisterModule(new KafkaAutofacModule());
            builder.RegisterOptions<BackplaneOptions>(_backplaneConfiguration);
        }
        if (_registerConfigChangesConsumer)
        {
            if (_backplaneConfiguration == null)
            {
                throw new InvalidOperationException("Backplane configuration should not be null.");
            }

            builder.RegisterType<ConfigChangesConsumer>().As<IConfigChangesConsumer>().SingleInstance();
            builder.RegisterFactory<KafkaConsumer<Null, ConfigChange>, IKafkaConsumer<Null, ConfigChange>>();
            builder.RegisterModule(new KafkaAutofacModule());
            builder.RegisterOptions<BackplaneOptions>(_backplaneConfiguration);
        }
        if (_registerHistory)
        {
            builder.RegisterType<HistoryConfig>().As<IHistoryConfig>().SingleInstance();
            builder.RegisterType<ConfigSection<HistoryValues>>()
                .As<IConfigSection<HistoryValues>>()
                .As<IConfigChangeSubscriber>()
                .SingleInstance();
            builder.RegisterType<HistoryRepo>().As<IRepo<HistoryValues>>().SingleInstance();
            builder.RegisterType<HistoryValidator>().As<IValidator<HistoryValues>>().SingleInstance();
        }
        if (_registerPartitioning)
        {
            builder.RegisterType<PartitioningConfig>().As<IPartitioningConfig>().SingleInstance();
            builder.RegisterType<ConfigSection<PartitioningValues>>()
                .As<IConfigSection<PartitioningValues>>()
                .As<IConfigChangeSubscriber>()
                .SingleInstance();
            builder.RegisterType<PartitioningRepo>().As<IRepo<PartitioningValues>>().SingleInstance();
            builder.RegisterType<PartitioningValidator>().As<IValidator<PartitioningValues>>().SingleInstance();
            builder.RegisterSingletonEvent<EventSource<PartitionsChangedEventArgs>, PartitionsChangedEventArgs>();
        }
        if (_registerSnowflake)
        {
            builder.RegisterType<SnowflakeConfig>().As<ISnowflakeConfig>().SingleInstance();
            builder.RegisterType<ConfigSection<SnowflakeValues>>()
                .As<IConfigSection<SnowflakeValues>>()
                .As<IConfigChangeSubscriber>()
                .SingleInstance();
            builder.RegisterType<SnowflakeRepo>().As<IRepo<SnowflakeValues>>().SingleInstance();
            builder.RegisterType<SnowflakeValidator>().As<IValidator<SnowflakeValues>>().SingleInstance();
        }
        if (_registerUser)
        {
            builder.RegisterType<UserConfig>().As<IUserConfig>().SingleInstance();
            builder.RegisterType<ConfigSection<UserValues>>()
                .As<IConfigSection<UserValues>>()
                .As<IConfigChangeSubscriber>()
                .SingleInstance();
            builder.RegisterType<UserRepo>().As<IRepo<UserValues>>().SingleInstance();
            builder.RegisterType<UserValidator>().As<IValidator<UserValues>>().SingleInstance();
        }
    }
}

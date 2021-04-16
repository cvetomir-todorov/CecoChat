using Autofac;
using CecoChat.Data.Configuration.History;
using CecoChat.Data.Configuration.Partitioning;
using CecoChat.Events;
using CecoChat.Redis;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.Configuration
{
    public sealed class ConfigurationDbAutofacModule : Module
    {
        public IConfiguration RedisConfiguration { get; init; }

        public bool RegisterHistory { get; init; } = false;

        public bool RegisterPartitioning { get; init; } = false;

        protected override void Load(ContainerBuilder builder)
        {
            if (RegisterHistory || RegisterPartitioning)
            {
                builder.RegisterModule(new RedisAutofacModule
                {
                    RedisConfiguration = RedisConfiguration
                });
                builder.RegisterType<ConfigurationUtility>().As<IConfigurationUtility>().SingleInstance();
            }
            if (RegisterHistory)
            {
                builder.RegisterType<HistoryConfiguration>().As<IHistoryConfiguration>().SingleInstance();
                builder.RegisterType<HistoryConfigurationRepository>().As<IHistoryConfigurationRepository>().SingleInstance();
            }
            if (RegisterPartitioning)
            {
                builder.RegisterType<PartitioningConfiguration>().As<IPartitioningConfiguration>().SingleInstance();
                builder.RegisterType<PartitioningConfigurationRepository>().As<IPartitioningConfigurationRepository>().SingleInstance();
                builder.RegisterSingletonEvent<EventSource<PartitionsChangedEventData>, PartitionsChangedEventData>();
            }
        }
    }
}

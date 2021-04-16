using Autofac;
using CecoChat.Data.Configuration.History;
using CecoChat.Data.Configuration.Partitioning;
using CecoChat.Events;
using CecoChat.Redis;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.Configuration
{
    public sealed class ConfigurationModule : Module
    {
        public IConfiguration RedisConfiguration { get; init; }

        public bool AddHistory { get; init; } = false;

        public bool AddPartitioning { get; init; } = false;

        protected override void Load(ContainerBuilder builder)
        {
            if (AddHistory || AddPartitioning)
            {
                builder.RegisterModule(new RedisModule
                {
                    RedisConfiguration = RedisConfiguration
                });
                builder.RegisterType<ConfigurationUtility>().As<IConfigurationUtility>().SingleInstance();
            }
            if (AddHistory)
            {
                builder.RegisterType<HistoryConfiguration>().As<IHistoryConfiguration>().SingleInstance();
                builder.RegisterType<HistoryConfigurationRepository>().As<IHistoryConfigurationRepository>().SingleInstance();
            }
            if (AddPartitioning)
            {
                builder.RegisterType<PartitioningConfiguration>().As<IPartitioningConfiguration>().SingleInstance();
                builder.RegisterType<PartitioningConfigurationRepository>().As<IPartitioningConfigurationRepository>().SingleInstance();
                builder.RegisterSingletonEvent<EventSource<PartitionsChangedEventData>, PartitionsChangedEventData>();
            }
        }
    }
}

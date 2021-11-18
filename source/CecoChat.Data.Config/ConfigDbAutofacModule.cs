using Autofac;
using CecoChat.Data.Config.History;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Events;
using CecoChat.Redis;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Data.Config
{
    public sealed class ConfigDbAutofacModule : Module
    {
        public IConfiguration RedisConfiguration { get; init; }

        public bool RegisterHistory { get; init; }

        public bool RegisterPartitioning { get; init; }

        protected override void Load(ContainerBuilder builder)
        {
            if (RegisterHistory || RegisterPartitioning)
            {
                builder.RegisterModule(new RedisAutofacModule
                {
                    RedisConfiguration = RedisConfiguration
                });
                builder.RegisterType<ConfigUtility>().As<IConfigUtility>().SingleInstance();
            }
            if (RegisterHistory)
            {
                builder.RegisterType<HistoryConfig>().As<IHistoryConfig>().SingleInstance();
                builder.RegisterType<HistoryConfigRepository>().As<IHistoryConfigRepository>().SingleInstance();
            }
            if (RegisterPartitioning)
            {
                builder.RegisterType<PartitioningConfig>().As<IPartitioningConfig>().SingleInstance();
                builder.RegisterType<PartitioningConfigRepository>().As<IPartitioningConfigRepository>().SingleInstance();
                builder.RegisterSingletonEvent<EventSource<PartitionsChangedEventData>, PartitionsChangedEventData>();
            }
        }
    }
}

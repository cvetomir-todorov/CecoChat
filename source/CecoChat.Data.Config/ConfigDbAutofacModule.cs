using Autofac;
using CecoChat.Data.Config.History;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Data.Config.Snowflake;
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

        public bool RegisterSnowflake { get; init; }

        protected override void Load(ContainerBuilder builder)
        {
            if (RegisterHistory || RegisterPartitioning || RegisterSnowflake)
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
                builder.RegisterType<HistoryConfigRepo>().As<IHistoryConfigRepo>().SingleInstance();
            }
            if (RegisterPartitioning)
            {
                builder.RegisterType<PartitioningConfig>().As<IPartitioningConfig>().SingleInstance();
                builder.RegisterType<PartitioningConfigRepo>().As<IPartitioningConfigRepo>().SingleInstance();
                builder.RegisterSingletonEvent<EventSource<PartitionsChangedEventData>, PartitionsChangedEventData>();
            }
            if (RegisterSnowflake)
            {
                builder.RegisterType<SnowflakeConfig>().As<ISnowflakeConfig>().SingleInstance();
                builder.RegisterType<SnowflakeConfigRepo>().As<ISnowflakeConfigRepo>().SingleInstance();
            }
        }
    }
}

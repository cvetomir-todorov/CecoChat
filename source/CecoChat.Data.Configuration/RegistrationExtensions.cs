using CecoChat.Data.Configuration.History;
using CecoChat.Data.Configuration.Partitioning;
using CecoChat.Events;
using CecoChat.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CecoChat.Data.Configuration
{
    public static class RegistrationExtensions
    {
        public static IServiceCollection AddConfiguration(
            this IServiceCollection services, 
            IConfiguration redisConfigurationSection,
            bool addHistory = false,
            bool addPartitioning = false)
        {
            if (addHistory || addPartitioning)
            {
                services = services
                    .AddRedis(redisConfigurationSection)
                    .AddSingleton<IConfigurationUtility, ConfigurationUtility>();
            }
            if (addHistory)
            {
                services = services
                    .AddSingleton<IHistoryConfiguration, HistoryConfiguration>()
                    .AddSingleton<IHistoryConfigurationRepository, HistoryConfigurationRepository>();
            }
            if (addPartitioning)
            {
                services = services
                    .AddSingleton<IPartitioningConfiguration, PartitioningConfiguration>()
                    .AddSingleton<IPartitioningConfigurationRepository, PartitioningConfigurationRepository>()
                    .AddEvent<EventSource<PartitionsChangedEventData>, PartitionsChangedEventData>();
            }

            return services;
        }
    }
}

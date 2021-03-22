using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Configuration.History;
using CecoChat.Data.Configuration.Partitioning;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Connect.Server.Initialization
{
    public sealed class ConfigurationHostedService : IHostedService
    {
        private readonly IPartitioningConfiguration _partitioningConfiguration;
        private readonly IHistoryConfiguration _historyConfiguration;

        public ConfigurationHostedService(
            IPartitioningConfiguration partitioningConfiguration,
            IHistoryConfiguration historyConfiguration)
        {
            _partitioningConfiguration = partitioningConfiguration;
            _historyConfiguration = historyConfiguration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _partitioningConfiguration.Initialize(new PartitioningConfigurationUsage
            {
                UseServerAddresses = true
            });
            await _historyConfiguration.Initialize(new HistoryConfigurationUsage
            {
                UseServerAddress = true
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

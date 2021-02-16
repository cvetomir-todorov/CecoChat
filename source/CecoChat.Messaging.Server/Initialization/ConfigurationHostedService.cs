using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Configuration.Partitioning;
using CecoChat.Messaging.Server.Backend;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Initialization
{
    public sealed class ConfigurationHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IPartitioningConfiguration _partitioningConfiguration;

        public ConfigurationHostedService(
            ILogger<ConfigurationHostedService> logger,
            IOptions<BackendOptions> backendOptions,
            IPartitioningConfiguration partitioningConfiguration)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _partitioningConfiguration = partitioningConfiguration;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Configured server ID is '{0}'.", _backendOptions.ServerID);

            await _partitioningConfiguration.Initialize(new PartitioningConfigurationUsage
            {
                UsePartitions = true,
                ServerForWhichToUsePartitions = _backendOptions.ServerID
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

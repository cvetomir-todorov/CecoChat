using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Messaging.Server.Backend;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Initialization
{
    public sealed class ConfigHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IPartitioningConfig _partitioningConfig;

        public ConfigHostedService(
            ILogger<ConfigHostedService> logger,
            IOptions<BackendOptions> backendOptions,
            IPartitioningConfig partitioningConfig)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _partitioningConfig = partitioningConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Configured server ID is '{0}'.", _backendOptions.ServerID);

            await _partitioningConfig.Initialize(new PartitioningConfigUsage
            {
                UseServerPartitions = true,
                ServerPartitionChangesToWatch = _backendOptions.ServerID
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Config.Partitioning;
using CecoChat.Server.Messaging.Backplane;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.HostedServices
{
    public sealed class InitDynamicConfig : IHostedService
    {
        private readonly ILogger _logger;
        private readonly BackplaneOptions _backplaneOptions;
        private readonly IPartitioningConfig _partitioningConfig;

        public InitDynamicConfig(
            ILogger<InitDynamicConfig> logger,
            IOptions<BackplaneOptions> backplaneOptions,
            IPartitioningConfig partitioningConfig)
        {
            _logger = logger;
            _backplaneOptions = backplaneOptions.Value;
            _partitioningConfig = partitioningConfig;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Configured server ID is '{0}'.", _backplaneOptions.ServerID);

            await _partitioningConfig.Initialize(new PartitioningConfigUsage
            {
                UseServerPartitions = true,
                ServerPartitionChangesToWatch = _backplaneOptions.ServerID
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

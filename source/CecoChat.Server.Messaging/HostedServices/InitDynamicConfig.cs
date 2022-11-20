using CecoChat.Data.Config.Partitioning;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.HostedServices;

public sealed class InitDynamicConfig : IHostedService
{
    private readonly ILogger _logger;
    private readonly ConfigOptions _configOptions;
    private readonly IPartitioningConfig _partitioningConfig;

    public InitDynamicConfig(
        ILogger<InitDynamicConfig> logger,
        IOptions<ConfigOptions> configOptions,
        IPartitioningConfig partitioningConfig)
    {
        _logger = logger;
        _configOptions = configOptions.Value;
        _partitioningConfig = partitioningConfig;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Configured server ID is {ServerId}", _configOptions.ServerId);

        await _partitioningConfig.Initialize(new PartitioningConfigUsage
        {
            UseServerPartitions = true,
            ServerPartitionChangesToWatch = _configOptions.ServerId
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

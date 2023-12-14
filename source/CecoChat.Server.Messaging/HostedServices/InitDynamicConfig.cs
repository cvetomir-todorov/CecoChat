using CecoChat.DynamicConfig.Partitioning;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.HostedServices;

public sealed class InitDynamicConfig : IHostedService
{
    private readonly ILogger _logger;
    private readonly ConfigOptions _configOptions;
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly DynamicConfigInitHealthCheck _dynamicConfigInitHealthCheck;

    public InitDynamicConfig(
        ILogger<InitDynamicConfig> logger,
        IOptions<ConfigOptions> configOptions,
        IPartitioningConfig partitioningConfig,
        DynamicConfigInitHealthCheck dynamicConfigInitHealthCheck)
    {
        _logger = logger;
        _configOptions = configOptions.Value;
        _partitioningConfig = partitioningConfig;
        _dynamicConfigInitHealthCheck = dynamicConfigInitHealthCheck;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Configured server ID is {ServerId}", _configOptions.ServerId);
        _dynamicConfigInitHealthCheck.IsReady = await _partitioningConfig.Initialize(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

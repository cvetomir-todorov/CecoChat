using CecoChat.Data.Config.Partitioning;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.HostedServices;

public sealed class InitDynamicConfig : IHostedService
{
    private readonly ILogger _logger;
    private readonly ConfigOptions _configOptions;
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly ConfigDbInitHealthCheck _configDbInitHealthCheck;

    public InitDynamicConfig(
        ILogger<InitDynamicConfig> logger,
        IOptions<ConfigOptions> configOptions,
        IPartitioningConfig partitioningConfig,
        ConfigDbInitHealthCheck configDbInitHealthCheck)
    {
        _logger = logger;
        _configOptions = configOptions.Value;
        _partitioningConfig = partitioningConfig;
        _configDbInitHealthCheck = configDbInitHealthCheck;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Configured server ID is {ServerId}", _configOptions.ServerId);
        _configDbInitHealthCheck.IsReady = await _partitioningConfig.Initialize();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

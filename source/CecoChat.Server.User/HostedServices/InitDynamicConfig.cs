using CecoChat.Data.Config.Partitioning;

namespace CecoChat.Server.User.HostedServices;

public class InitDynamicConfig : IHostedService
{
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly ConfigDbInitHealthCheck _configDbInitHealthCheck;

    public InitDynamicConfig(
        IPartitioningConfig partitioningConfig,
        ConfigDbInitHealthCheck configDbInitHealthCheck)
    {
        _partitioningConfig = partitioningConfig;
        _configDbInitHealthCheck = configDbInitHealthCheck;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _configDbInitHealthCheck.IsReady = await _partitioningConfig.Initialize(new PartitioningConfigUsage());
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

using CecoChat.Data.Config.Partitioning;

namespace CecoChat.Server.Bff.HostedServices;

public sealed class InitDynamicConfig : IHostedService
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
        await _partitioningConfig.Initialize(new PartitioningConfigUsage
        {
            UseAddresses = true
        });

        _configDbInitHealthCheck.IsReady = true;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

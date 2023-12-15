using CecoChat.DynamicConfig.Partitioning;

namespace CecoChat.Server.Bff.HostedServices;

public sealed class InitDynamicConfig : IHostedService
{
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly DynamicConfigInitHealthCheck _dynamicConfigInitHealthCheck;

    public InitDynamicConfig(
        IPartitioningConfig partitioningConfig,
        DynamicConfigInitHealthCheck dynamicConfigInitHealthCheck)
    {
        _partitioningConfig = partitioningConfig;
        _dynamicConfigInitHealthCheck = dynamicConfigInitHealthCheck;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _dynamicConfigInitHealthCheck.IsReady = await _partitioningConfig.Initialize(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

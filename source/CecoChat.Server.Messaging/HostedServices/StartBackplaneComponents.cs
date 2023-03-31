using CecoChat.Data.Config.Partitioning;
using CecoChat.Kafka;
using CecoChat.Server.Messaging.Backplane;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.HostedServices;

public sealed class StartBackplaneComponents : IHostedService, IDisposable
{
    private readonly ConfigOptions _configOptions;
    private readonly IBackplaneComponents _backplaneComponents;
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly CancellationToken _appStoppingCt;
    private CancellationTokenSource? _stoppedCts;

    public StartBackplaneComponents(
        IHostApplicationLifetime applicationLifetime,
        IOptions<ConfigOptions> configOptions,
        IBackplaneComponents backplaneComponents,
        IPartitioningConfig partitioningConfig)
    {
        _configOptions = configOptions.Value;
        _backplaneComponents = backplaneComponents;
        _partitioningConfig = partitioningConfig;

        _appStoppingCt = applicationLifetime.ApplicationStopping;
    }

    public void Dispose()
    {
        _stoppedCts?.Dispose();
        _backplaneComponents.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _appStoppingCt);

        int partitionCount = _partitioningConfig.PartitionCount;
        PartitionRange partitions = _partitioningConfig.GetPartitions(_configOptions.ServerId);

        _backplaneComponents.ConfigurePartitioning(partitionCount, partitions);
        _backplaneComponents.StartConsumption(_stoppedCts.Token);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

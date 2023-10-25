using CecoChat.Data.Config.Partitioning;
using CecoChat.Kafka;
using CecoChat.Server.User.Backplane;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.User.HostedServices;

public sealed class InitBackplane : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
    private readonly IConnectionNotifyProducer _connectionNotifyProducer;
    private readonly CancellationToken _appStoppingCt;
    private CancellationTokenSource? _stoppedCts;

    public InitBackplane(
        IHostApplicationLifetime applicationLifetime,
        ILogger<InitBackplane> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        IPartitioningConfig partitioningConfig,
        ITopicPartitionFlyweight topicPartitionFlyweight,
        IConnectionNotifyProducer connectionNotifyProducer)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _partitioningConfig = partitioningConfig;
        _topicPartitionFlyweight = topicPartitionFlyweight;
        _connectionNotifyProducer = connectionNotifyProducer;

        _appStoppingCt = applicationLifetime.ApplicationStopping;
    }

    public void Dispose()
    {
        _stoppedCts?.Dispose();
    }

    // TODO: handle partition count change
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _appStoppingCt);

        int partitionCount = _partitioningConfig.PartitionCount;
        _topicPartitionFlyweight.Add(_backplaneOptions.TopicMessagesByReceiver, partitionCount);
        _connectionNotifyProducer.PartitionCount = partitionCount;

        _logger.LogInformation("Prepared backplane components for topic {TopicMessagesByReceiver} with {PartitionCount} partitions", 
            _backplaneOptions.TopicMessagesByReceiver, partitionCount);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

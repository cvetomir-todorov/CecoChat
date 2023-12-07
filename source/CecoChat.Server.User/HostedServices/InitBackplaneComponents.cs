using CecoChat.Data.Config.Partitioning;
using CecoChat.Events;
using CecoChat.Kafka;
using CecoChat.Server.User.Backplane;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.User.HostedServices;

public sealed class InitBackplaneComponents : IHostedService, ISubscriber<EventArgs>, IDisposable
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
    private readonly IConnectionNotifyProducer _connectionNotifyProducer;
    private readonly IEvent<EventArgs> _partitionsChanged;
    private readonly Guid _partitionsChangedToken;
    private readonly CancellationToken _appStoppingCt;
    private CancellationTokenSource? _stoppedCts;

    public InitBackplaneComponents(
        IHostApplicationLifetime applicationLifetime,
        ILogger<InitBackplaneComponents> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        IPartitioningConfig partitioningConfig,
        ITopicPartitionFlyweight topicPartitionFlyweight,
        IConnectionNotifyProducer connectionNotifyProducer,
        IEvent<EventArgs> partitionsChanged)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _partitioningConfig = partitioningConfig;
        _topicPartitionFlyweight = topicPartitionFlyweight;
        _connectionNotifyProducer = connectionNotifyProducer;
        _partitionsChanged = partitionsChanged;

        _partitionsChangedToken = _partitionsChanged.Subscribe(this);
        _appStoppingCt = applicationLifetime.ApplicationStopping;
    }

    public void Dispose()
    {
        _stoppedCts?.Dispose();
        _partitionsChanged.Unsubscribe(_partitionsChangedToken);
    }

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

    public ValueTask Handle(EventArgs _)
    {
        int newPartitionCount = _partitioningConfig.PartitionCount;
        int currentPartitionCount = _topicPartitionFlyweight.GetTopicPartitionCount(_backplaneOptions.TopicMessagesByReceiver);

        if (currentPartitionCount < newPartitionCount)
        {
            _topicPartitionFlyweight.AddOrUpdate(_backplaneOptions.TopicMessagesByReceiver, newPartitionCount);
            _logger.LogInformation("Increased cached partitions for topic {Topic} from {CurrentPartitionCount} to {NewPartitionCount}",
                _backplaneOptions.TopicMessagesByReceiver, currentPartitionCount, newPartitionCount);
        }

        _connectionNotifyProducer.PartitionCount = newPartitionCount;

        return ValueTask.CompletedTask;
    }
}

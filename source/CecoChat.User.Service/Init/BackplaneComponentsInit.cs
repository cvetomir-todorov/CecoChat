using CecoChat.Config.Backplane;
using CecoChat.Config.Sections.Partitioning;
using CecoChat.Server;
using CecoChat.User.Service.Backplane;
using Common.AspNet.Init;
using Common.Events;
using Common.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.User.Service.Init;

public sealed class BackplaneComponentsInit : InitStep, ISubscriber<PartitionsChangedEventArgs>
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
    private readonly IConnectionNotifyProducer _connectionNotifyProducer;
    private readonly IConfigChangesConsumer _configChangesConsumer;
    private readonly ConfigChangesConsumerHealthCheck _configChangesConsumerHealthCheck;
    private readonly IEvent<PartitionsChangedEventArgs> _partitionsChanged;
    private readonly Guid _partitionsChangedToken;

    public BackplaneComponentsInit(
        IHostApplicationLifetime applicationLifetime,
        ILogger<BackplaneComponentsInit> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        IPartitioningConfig partitioningConfig,
        ITopicPartitionFlyweight topicPartitionFlyweight,
        IConnectionNotifyProducer connectionNotifyProducer,
        IConfigChangesConsumer configChangesConsumer,
        ConfigChangesConsumerHealthCheck configChangesConsumerHealthCheck,
        IEvent<PartitionsChangedEventArgs> partitionsChanged)
        : base(applicationLifetime)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _partitioningConfig = partitioningConfig;
        _topicPartitionFlyweight = topicPartitionFlyweight;
        _connectionNotifyProducer = connectionNotifyProducer;
        _configChangesConsumer = configChangesConsumer;
        _configChangesConsumerHealthCheck = configChangesConsumerHealthCheck;
        _partitionsChanged = partitionsChanged;

        _partitionsChangedToken = _partitionsChanged.Subscribe(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _partitionsChanged.Unsubscribe(_partitionsChangedToken);
            _configChangesConsumer.Dispose();
        }
    }

    protected override Task<bool> DoExecute(CancellationToken ct)
    {
        _configChangesConsumer.Prepare();
        StartConfigChangesConsumer(ct);

        int partitionCount = _partitioningConfig.PartitionCount;
        _topicPartitionFlyweight.Add(_backplaneOptions.TopicMessagesByReceiver, partitionCount);
        _connectionNotifyProducer.PartitionCount = partitionCount;

        _logger.LogInformation("Prepared backplane components for topic {TopicMessagesByReceiver} with {PartitionCount} partitions",
            _backplaneOptions.TopicMessagesByReceiver, partitionCount);

        return Task.FromResult(true);
    }

    private void StartConfigChangesConsumer(CancellationToken ct)
    {
        Task.Factory.StartNew(() =>
        {
            try
            {
                _configChangesConsumerHealthCheck.IsReady = true;
                _configChangesConsumer.Start(ct);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failure in config changes consumer");
            }
            finally
            {
                _configChangesConsumerHealthCheck.IsReady = false;
            }
        }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }

    public ValueTask Handle(PartitionsChangedEventArgs _)
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

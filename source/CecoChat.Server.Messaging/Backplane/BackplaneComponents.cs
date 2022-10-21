using CecoChat.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.Backplane;

public interface IBackplaneComponents : IDisposable
{
    void ConfigurePartitioning(int partitionCount, PartitionRange partitions);

    void StartConsumption(CancellationToken ct);
}

public sealed class BackplaneComponents : IBackplaneComponents
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
    private readonly ISendersProducer _sendersProducer;
    private readonly IReceiversConsumer _receiversConsumer;
    private readonly IMessageReplicator _messageReplicator;

    public BackplaneComponents(
        ILogger<BackplaneComponents> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        ITopicPartitionFlyweight topicPartitionFlyweight,
        ISendersProducer sendersProducer,
        IReceiversConsumer receiversConsumer,
        IMessageReplicator messageReplicator)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _topicPartitionFlyweight = topicPartitionFlyweight;
        _sendersProducer = sendersProducer;
        _receiversConsumer = receiversConsumer;
        _messageReplicator = messageReplicator;
    }

    public void Dispose()
    {
        _sendersProducer.Dispose();
        _receiversConsumer.Dispose();
        _messageReplicator.Dispose();
    }

    public void ConfigurePartitioning(int partitionCount, PartitionRange partitions)
    {
        int currentPartitionCount = _topicPartitionFlyweight.GetTopicPartitionCount(_backplaneOptions.TopicMessagesByReceiver);
        if (currentPartitionCount < partitionCount)
        {
            _topicPartitionFlyweight.AddOrUpdate(_backplaneOptions.TopicMessagesByReceiver, partitionCount);
            _logger.LogInformation("Increase cached partitions for topic {Topic} from {CurrentPartitionCount} to {NewPartitionCount}",
                _backplaneOptions.TopicMessagesByReceiver, currentPartitionCount, partitionCount);
        }

        currentPartitionCount = _topicPartitionFlyweight.GetTopicPartitionCount(_backplaneOptions.TopicMessagesBySender);
        if (currentPartitionCount < partitionCount)
        {
            _topicPartitionFlyweight.AddOrUpdate(_backplaneOptions.TopicMessagesBySender, partitionCount);
            _logger.LogInformation("Increase cached partitions for topic {Topic} from {CurrentPartitionCount} to {NewPartitionCount}",
                _backplaneOptions.TopicMessagesBySender, currentPartitionCount, partitionCount);
        }

        _sendersProducer.PartitionCount = partitionCount;
        _receiversConsumer.Prepare(partitions);
        _messageReplicator.PartitionCount = partitionCount;
        _messageReplicator.Prepare(partitions);

        _logger.LogInformation("Prepared backplane components for topics {TopicMessagesByReceiver} and {TopicMessagesBySender} to use partitions {Partitions}",
            _backplaneOptions.TopicMessagesByReceiver, _backplaneOptions.TopicMessagesBySender, partitions);
    }

    public void StartConsumption(CancellationToken ct)
    {
        Task.Factory.StartNew(() =>
        {
            try
            {
                _receiversConsumer.Start(ct);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failure in consumer {ConsumerId}", _receiversConsumer.ConsumerID);
            }
        }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);

        Task.Factory.StartNew(() =>
        {
            try
            {
                _messageReplicator.Start(ct);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failure in consumer {ConsumerId}", _messageReplicator.ConsumerID);
            }
        }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }
}
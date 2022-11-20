using CecoChat.Kafka;
using CecoChat.Threading;
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
    private DedicatedThreadTaskScheduler? _receiversConsumerTaskScheduler;

    public BackplaneComponents(
        ILogger<BackplaneComponents> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        ITopicPartitionFlyweight topicPartitionFlyweight,
        ISendersProducer sendersProducer,
        IReceiversConsumer receiversConsumer)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _topicPartitionFlyweight = topicPartitionFlyweight;
        _sendersProducer = sendersProducer;
        _receiversConsumer = receiversConsumer;
    }

    public void Dispose()
    {
        _receiversConsumerTaskScheduler?.Dispose();
        _sendersProducer.Dispose();
        _receiversConsumer.Dispose();
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

        _sendersProducer.PartitionCount = partitionCount;
        _receiversConsumer.Prepare(partitions);

        _logger.LogInformation("Prepared backplane components for topic {TopicMessagesByReceiver} to use partitions {Partitions}",
            _backplaneOptions.TopicMessagesByReceiver, partitions);
    }

    public void StartConsumption(CancellationToken ct)
    {
        _receiversConsumerTaskScheduler = new DedicatedThreadTaskScheduler();
        Task.Factory.StartNew(() =>
        {
            try
            {
                _receiversConsumer.Start(ct);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failure in consumer {ConsumerId}", _receiversConsumer.ConsumerId);
            }
        }, ct, TaskCreationOptions.LongRunning, _receiversConsumerTaskScheduler);
    }
}

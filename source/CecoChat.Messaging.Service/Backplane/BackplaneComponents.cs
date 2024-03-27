using CecoChat.DynamicConfig.Backplane;
using CecoChat.Server;
using Common.Kafka;
using Common.Threading;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Service.Backplane;

public interface IBackplaneComponents : IDisposable
{
    void ConfigurePartitioning(int partitionCount, PartitionRange partitions);

    int CurrentPartitionCount { get; }

    PartitionRange CurrentPartitions { get; }

    void StartConsumption(CancellationToken ct);
}

public sealed class BackplaneComponents : IBackplaneComponents
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
    private readonly ISendersProducer _sendersProducer;
    private readonly IReceiversConsumer _receiversConsumer;
    private readonly IConfigChangesConsumer _configChangesConsumer;
    private readonly ReceiversConsumerHealthCheck _receiversConsumerHealthCheck;
    private readonly ConfigChangesConsumerHealthCheck _configChangesConsumerHealthCheck;
    private DedicatedThreadTaskScheduler? _receiversConsumerTaskScheduler;

    public BackplaneComponents(
        ILogger<BackplaneComponents> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        ITopicPartitionFlyweight topicPartitionFlyweight,
        ISendersProducer sendersProducer,
        IReceiversConsumer receiversConsumer,
        IConfigChangesConsumer configChangesConsumer,
        ReceiversConsumerHealthCheck receiversConsumerHealthCheck,
        ConfigChangesConsumerHealthCheck configChangesConsumerHealthCheck)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _topicPartitionFlyweight = topicPartitionFlyweight;
        _sendersProducer = sendersProducer;
        _receiversConsumer = receiversConsumer;
        _configChangesConsumer = configChangesConsumer;
        _receiversConsumerHealthCheck = receiversConsumerHealthCheck;
        _configChangesConsumerHealthCheck = configChangesConsumerHealthCheck;
    }

    public void Dispose()
    {
        _receiversConsumerTaskScheduler?.Dispose();
        _sendersProducer.Dispose();
        _receiversConsumer.Dispose();
        _configChangesConsumer.Dispose();
    }

    public void ConfigurePartitioning(int partitionCount, PartitionRange partitions)
    {
        int currentPartitionCount = _topicPartitionFlyweight.GetTopicPartitionCount(_backplaneOptions.TopicMessagesByReceiver);
        if (currentPartitionCount < partitionCount)
        {
            _topicPartitionFlyweight.AddOrUpdate(_backplaneOptions.TopicMessagesByReceiver, partitionCount);
            _logger.LogInformation("Increased cached partitions for topic {Topic} from {CurrentPartitionCount} to {NewPartitionCount}",
                _backplaneOptions.TopicMessagesByReceiver, currentPartitionCount, partitionCount);
        }

        _sendersProducer.PartitionCount = partitionCount;
        _receiversConsumer.Prepare(partitions);

        CurrentPartitionCount = partitionCount;
        CurrentPartitions = partitions;

        _logger.LogInformation("Prepared backplane components for topic {TopicMessagesByReceiver} to use partitions {Partitions} from {PartitionCount} partitions",
            _backplaneOptions.TopicMessagesByReceiver, partitions, partitionCount);
    }

    public int CurrentPartitionCount { get; private set; }

    public PartitionRange CurrentPartitions { get; private set; }

    public void StartConsumption(CancellationToken ct)
    {
        StartConfigChangesConsumer(ct);
        StartReceiversConsumer(ct);
    }

    private void StartConfigChangesConsumer(CancellationToken ct)
    {
        _configChangesConsumer.Prepare();
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

    private void StartReceiversConsumer(CancellationToken ct)
    {
        _receiversConsumerTaskScheduler = new DedicatedThreadTaskScheduler();
        Task.Factory.StartNew(() =>
        {
            try
            {
                _receiversConsumerHealthCheck.IsReady = true;
                _receiversConsumer.Start(ct);
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Failure in consumer {ConsumerId}", _receiversConsumer.ConsumerId);
            }
            finally
            {
                _receiversConsumerHealthCheck.IsReady = false;
            }
        }, ct, TaskCreationOptions.LongRunning, _receiversConsumerTaskScheduler);
    }
}

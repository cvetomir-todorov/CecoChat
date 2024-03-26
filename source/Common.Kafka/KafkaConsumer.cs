using System.Diagnostics;
using Common.Kafka.Telemetry;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Common.Kafka;

public interface IKafkaConsumer<TKey, TValue> : IDisposable
{
    void Initialize(KafkaOptions options, KafkaConsumerOptions consumerOptions, IDeserializer<TValue> valueDeserializer);

    void Subscribe(string topic);

    void Assign(string topic, PartitionRange partitions, ITopicPartitionFlyweight partitionFlyweight);

    void Consume(Action<ConsumeResult<TKey, TValue>> messageHandler, CancellationToken ct);
}

public sealed class KafkaConsumer<TKey, TValue> : IKafkaConsumer<TKey, TValue>
{
    private readonly ILogger _logger;
    private readonly IKafkaTelemetry _kafkaTelemetry;
    private PartitionRange _assignedPartitions;
    private IConsumer<TKey, TValue>? _consumer;
    private KafkaConsumerOptions? _consumerOptions;
    private string _id;
    private bool _isDisposed;

    public KafkaConsumer(
        ILogger<KafkaConsumer<TKey, TValue>> logger,
        IKafkaTelemetry kafkaTelemetry)
    {
        _logger = logger;
        _kafkaTelemetry = kafkaTelemetry;

        _assignedPartitions = PartitionRange.Empty;
        _id = "non-initialized";
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _consumer?.Close();
            _isDisposed = true;
        }
    }

    public void Initialize(KafkaOptions options, KafkaConsumerOptions consumerOptions, IDeserializer<TValue> valueDeserializer)
    {
        if (_consumer != null)
        {
            throw new InvalidOperationException($"'{nameof(Initialize)}' already called.");
        }

        ConsumerConfig configuration = new()
        {
            BootstrapServers = string.Join(separator: ',', options.BootstrapServers),
            GroupId = consumerOptions.ConsumerGroupId,
            AutoOffsetReset = consumerOptions.AutoOffsetReset,
            EnablePartitionEof = consumerOptions.EnablePartitionEof,
            AllowAutoCreateTopics = consumerOptions.AllowAutoCreateTopics,
            EnableAutoCommit = consumerOptions.EnableAutoCommit
        };

        _consumer = new ConsumerBuilder<TKey, TValue>(configuration)
            .SetValueDeserializer(valueDeserializer)
            .Build();
        _consumerOptions = consumerOptions;
        _id = $"{consumerOptions.ConsumerGroupId}-{KafkaConsumerIdGenerator.GetNextId()}";
    }

    public void Subscribe(string topic)
    {
        EnsureInitialized();
        _consumer!.Subscribe(topic);
        _logger.LogInformation("Consumer {ConsumerId} subscribed to topic {Topic}", _id, topic);
    }

    public void Assign(string topic, PartitionRange partitions, ITopicPartitionFlyweight partitionFlyweight)
    {
        EnsureInitialized();

        if (_assignedPartitions.Equals(partitions))
        {
            _logger.LogInformation("Consumer {ConsumerId} already assigned partitions {Partitions} from topic {Topic}", _id, _assignedPartitions, topic);
            return;
        }

        List<TopicPartition> topicPartitions = new(capacity: partitions.Length);
        for (int partition = partitions.Lower; partition <= partitions.Upper; ++partition)
        {
            TopicPartition topicPartition = partitionFlyweight.GetTopicPartition(topic, partition);
            topicPartitions.Add(topicPartition);
        }

        _consumer!.Assign(topicPartitions);
        _assignedPartitions = partitions;
        _logger.LogInformation("Consumer {ConsumerId} assigned partitions {Partitions} from topic {Topic}", _id, partitions, topic);
    }

    private enum ConsumeStage
    {
        Initial, AfterConsume, AfterMessageHandle, AfterCommit
    }

    public void Consume(Action<ConsumeResult<TKey, TValue>> messageHandler, CancellationToken ct)
    {
        EnsureInitialized();

        ConsumeStage stage = ConsumeStage.Initial;
        ConsumeResult<TKey, TValue> consumeResult = new();
        Activity? activity = default;
        Exception? activityException = default;

        try
        {
            if (!DoConsume(messageHandler, out Exception? messageHandlerException, out stage, out consumeResult, out activity, ct))
            {
                throw new InvalidOperationException("Message handler threw an exception.", messageHandlerException);
            }
        }
        catch (OperationCanceledException)
        {
            // nothing more to do when the ct was canceled
        }
        catch (AccessViolationException accessViolationException)
        {
            HandleConsumerDisposal(accessViolationException, ct);
        }
        catch (ObjectDisposedException objectDisposedException)
        {
            HandleConsumerDisposal(objectDisposedException, ct);
        }
        catch (Exception exception)
        {
            activityException = exception;
            _logger.LogError(exception, "Consumer {ConsumerId} error", _id);
        }
        finally
        {
            bool success = InspectStage(stage, consumeResult);
            _kafkaTelemetry.StopConsumer(activity, success, activityException);
        }
    }

    private void EnsureInitialized()
    {
        if (_consumer == null || _consumerOptions == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the consumer.");
        }
    }

    private void HandleConsumerDisposal(Exception exception, CancellationToken ct)
    {
        if (!ct.IsCancellationRequested)
        {
            _logger.LogError(exception, "Consumer {ConsumerId} was disposed without cancellation being requested", _id);
        }
    }

    private bool DoConsume(
        Action<ConsumeResult<TKey, TValue>> messageHandler,
        out Exception? messageHandlerException,
        out ConsumeStage stage,
        out ConsumeResult<TKey, TValue> consumeResult,
        out Activity? activity,
        CancellationToken ct)
    {
        messageHandlerException = default;
        consumeResult = _consumer!.Consume(ct);
        // consume blocks until there is a message and it is read => start activity after that
        activity = _kafkaTelemetry.StartConsumer(consumeResult, _consumerOptions!.ConsumerGroupId);
        stage = ConsumeStage.AfterConsume;

        try
        {
            messageHandler(consumeResult);
            stage = ConsumeStage.AfterMessageHandle;
        }
        catch (Exception e)
        {
            messageHandlerException = e;
        }

        if (messageHandlerException != default)
        {
            return false;
        }

        if (!_consumerOptions.EnableAutoCommit)
        {
            _consumer.Commit(consumeResult);
        }
        stage = ConsumeStage.AfterCommit;
        return true;
    }

    private bool InspectStage(ConsumeStage stage, ConsumeResult<TKey, TValue> consumeResult)
    {
        bool success = false;

        switch (stage)
        {
            case ConsumeStage.Initial:
                _logger.LogError("Consumer {ConsumerId} failed to consume a message", _id);
                break;
            case ConsumeStage.AfterConsume:
                _logger.LogError("Consumer {ConsumerId} encountered a failing message handler", _id);
                break;
            case ConsumeStage.AfterMessageHandle:
                _logger.LogError("Consumer {ConsumerId} failed to commit to topic {Topic} partition {Partition} offset {Offset}",
                    _id, consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);
                break;
            case ConsumeStage.AfterCommit:
                success = true;
                break;
            default:
                throw new EnumValueNotSupportedException(stage);
        }

        return success;
    }
}

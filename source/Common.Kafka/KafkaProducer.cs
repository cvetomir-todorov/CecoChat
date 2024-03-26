using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Common.Kafka.Telemetry;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Common.Kafka;

public interface IKafkaProducer<TKey, TValue> : IDisposable
{
    void Initialize(KafkaOptions options, KafkaProducerOptions producerOptions, ISerializer<TValue> valueSerializer);

    void Produce(Message<TKey, TValue> message, TopicPartition topicPartition, DeliveryHandler<TKey, TValue>? deliveryHandler = null);

    void Produce(Message<TKey, TValue> message, string topic, DeliveryHandler<TKey, TValue>? deliveryHandler = null);

    void FlushPendingMessages();
}

public delegate void DeliveryHandler<TKey, TValue>(bool isDelivered, DeliveryReport<TKey, TValue> report, Activity? produceActivity);

public sealed class KafkaProducer<TKey, TValue> : IKafkaProducer<TKey, TValue>
{
    private readonly ILogger _logger;
    private readonly IKafkaTelemetry _kafkaTelemetry;
    private IProducer<TKey, TValue>? _producer;
    private KafkaProducerOptions? _producerOptions;
    private string _id;
    private bool _isDisposed;

    public KafkaProducer(
        ILogger<KafkaProducer<TKey, TValue>> logger,
        IKafkaTelemetry kafkaTelemetry)
    {
        _logger = logger;
        _kafkaTelemetry = kafkaTelemetry;
        _id = "non-initialized";
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _producer?.Dispose();
            _isDisposed = true;
        }
    }

    public void Initialize(KafkaOptions options, KafkaProducerOptions producerOptions, ISerializer<TValue> valueSerializer)
    {
        if (_producer != null)
        {
            throw new InvalidOperationException($"'{nameof(Initialize)}' already called.");
        }

        ProducerConfig configuration = new()
        {
            BootstrapServers = string.Join(separator: ',', options.BootstrapServers),
            Acks = producerOptions.Acks,
            LingerMs = producerOptions.LingerMs,
            MessageTimeoutMs = producerOptions.MessageTimeoutMs,
            MessageSendMaxRetries = producerOptions.MessageSendMaxRetries
        };

        _producer = new ProducerBuilder<TKey, TValue>(configuration)
            .SetValueSerializer(valueSerializer)
            .Build();
        _producerOptions = producerOptions;
        _id = $"{producerOptions.ProducerId}-{KafkaProducerIdGenerator.GetNextId()}";
    }

    public void Produce(Message<TKey, TValue> message, TopicPartition topicPartition, DeliveryHandler<TKey, TValue>? deliveryHandler = null)
    {
        EnsureInitialized();

        string topic = topicPartition.Topic;
        int partition = topicPartition.Partition;

        Activity? activity = _kafkaTelemetry.StartProducer(message, _producerOptions!.ProducerId, topic, partition);
        try
        {
            _producer.Produce(topicPartition, message, deliveryReport => HandleDeliveryReport(deliveryReport, activity, deliveryHandler));
        }
        catch (Exception exception)
        {
            _kafkaTelemetry.StopProducer(activity, success: false, exception);
            throw;
        }
    }

    public void Produce(Message<TKey, TValue> message, string topic, DeliveryHandler<TKey, TValue>? deliveryHandler = null)
    {
        EnsureInitialized();

        Activity? activity = _kafkaTelemetry.StartProducer(message, _producerOptions!.ProducerId, topic);
        try
        {
            _producer.Produce(topic, message, deliveryReport => HandleDeliveryReport(deliveryReport, activity, deliveryHandler));
        }
        catch (Exception exception)
        {
            _kafkaTelemetry.StopProducer(activity, success: false, exception);
            throw;
        }
    }

    [MemberNotNull(nameof(_producer))]
    private void EnsureInitialized()
    {
        if (_producer == null || _producerOptions == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the producer.");
        }
    }

    public void FlushPendingMessages()
    {
        if (_producer == null)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Producer {ProducerId} flushing pending messages...", _id);
            _producer.Flush();
            _logger.LogInformation("Producer {ProducerId} flushing pending messages succeeded", _id);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Producer {ProducerId} flushing pending messages failed", _id);
        }
    }

    private void HandleDeliveryReport(DeliveryReport<TKey, TValue> report, Activity? activity, DeliveryHandler<TKey, TValue>? deliveryHandler)
    {
        bool isDelivered = true;
        try
        {
            DateTime timestamp = report.Message.Timestamp.UtcDateTime;

            if (report.Status != PersistenceStatus.Persisted)
            {
                _logger.LogError("Message with timestamp {MessageTimestamp} has persistence status {Status}", timestamp, report.Status);
                isDelivered = false;
            }
            if (report.Error.IsError)
            {
                _logger.LogError("Message with timestamp {MessageTimestamp} has error code {ErrorCode} with reason '{ErrorReason}'", timestamp, report.Error.Code, report.Error.Reason);
                isDelivered = false;
            }
            if (report.TopicPartitionOffsetError.Error.IsError)
            {
                _logger.LogError("Message with timestamp {MessageTimestamp} in topic partition {Partition} has error code {ErrorCode} with reason '{ErrorReason}'",
                    timestamp, report.TopicPartitionOffsetError.Partition, report.Error.Code, report.TopicPartitionOffsetError.Error.Reason);
                isDelivered = false;
            }

            deliveryHandler?.Invoke(isDelivered, report, activity);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to handle delivery report for message {MessageKey}->{MessageValue}", report.Message.Key, report.Message.Value);
        }
        finally
        {
            _kafkaTelemetry.StopProducer(activity, isDelivered, exception: null);
        }
    }
}

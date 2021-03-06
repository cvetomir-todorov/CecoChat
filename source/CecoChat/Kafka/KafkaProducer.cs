﻿using System;
using System.Diagnostics;
using System.Threading;
using CecoChat.Kafka.Instrumentation;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace CecoChat.Kafka
{
    public interface IKafkaProducer<TKey, TValue> : IDisposable
    {
        void Initialize(IKafkaOptions options, IKafkaProducerOptions producerOptions, ISerializer<TValue> valueSerializer);

        void Produce(Message<TKey, TValue> message, TopicPartition topicPartition, DeliveryHandler<TKey, TValue> deliveryHandler = null);

        void Produce(Message<TKey, TValue> message, string topic, DeliveryHandler<TKey, TValue> deliveryHandler = null);

        void FlushPendingMessages();
    }

    public delegate void DeliveryHandler<TKey, TValue>(bool isDelivered, DeliveryReport<TKey, TValue> report, Activity activity);

    public sealed class KafkaProducer<TKey, TValue> : IKafkaProducer<TKey, TValue>
    {
        private readonly ILogger _logger;
        private readonly IKafkaActivityUtility _kafkaActivityUtility;
        private IProducer<TKey, TValue> _producer;
        private IKafkaProducerOptions _producerOptions;
        private string _id;
        private bool _isDisposed;

        public KafkaProducer(
            ILogger<KafkaProducer<TKey, TValue>> logger,
            IKafkaActivityUtility kafkaActivityUtility)
        {
            _logger = logger;
            _kafkaActivityUtility = kafkaActivityUtility;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _producer?.Dispose();
                _isDisposed = true;
            }
        }

        public void Initialize(IKafkaOptions options, IKafkaProducerOptions producerOptions, ISerializer<TValue> valueSerializer)
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
            _id = $"{KafkaProducerIDGenerator.GetNextID()}@{producerOptions.ProducerID}";
        }

        public void Produce(Message<TKey, TValue> message, TopicPartition topicPartition, DeliveryHandler<TKey, TValue> deliveryHandler = null)
        {
            string topic = topicPartition.Topic;
            int partition = topicPartition.Partition;

            Activity activity = _kafkaActivityUtility.StartProducer(message, _producerOptions.ProducerID, topic, partition);
            _producer.Produce(topicPartition, message, deliveryReport => HandleDeliveryReport(deliveryReport, activity, deliveryHandler));
            _logger.LogTrace("Producer {0} produced message {1} in {2}[{3}].", _id, message.Value, topic, partition);
        }

        public void Produce(Message<TKey, TValue> message, string topic, DeliveryHandler<TKey, TValue> deliveryHandler = null)
        {
            Activity activity = _kafkaActivityUtility.StartProducer(message, _producerOptions.ProducerID, topic);
            _producer.Produce(topic, message, deliveryReport => HandleDeliveryReport(deliveryReport, activity, deliveryHandler));
            _logger.LogTrace("Producer {0} produced message {1} in {2}.", _id, message.Value, topic);
        }

        public void FlushPendingMessages()
        {
            if (_producer == null)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Producer {0} flushing pending messages...", _id);
                _producer.Flush();
                _logger.LogInformation("Producer {0} flushing pending messages succeeded.", _id);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Producer {0} flushing pending messages failed.", _id);
            }
        }

        private void HandleDeliveryReport(DeliveryReport<TKey, TValue> report, Activity activity, DeliveryHandler<TKey, TValue> deliveryHandler)
        {
            bool isDelivered = true;
            try
            {
                TValue value = report.Message.Value;

                if (report.Status != PersistenceStatus.Persisted)
                {
                    _logger.LogError("Message {0} persistence status {1}.", value, report.Status);
                    isDelivered = false;
                }
                if (report.Error.IsError)
                {
                    _logger.LogError("Message {0} error code {1} reason '{2}'.", value, report.Error.Code, report.Error.Reason);
                    isDelivered = false;
                }
                if (report.TopicPartitionOffsetError.Error.IsError)
                {
                    _logger.LogError("Message {0} topic partition {1} error code {2} reason '{3}'.",
                        value, report.TopicPartitionOffsetError.Partition, report.Error.Code, report.TopicPartitionOffsetError.Error.Reason);
                    isDelivered = false;
                }

                deliveryHandler?.Invoke(isDelivered, report, activity);
            }
            finally
            {
                _kafkaActivityUtility.StopProducer(activity, isDelivered);
            }
        }
    }

    /// <summary>
    /// Not inside the <see cref="KafkaProducer{TKey,TValue}"/> class which uses it since it is generic.
    /// </summary>
    internal static class KafkaProducerIDGenerator
    {
        private static int _nextIDCounter;

        public static int GetNextID()
        {
            return Interlocked.Increment(ref _nextIDCounter);
        }
    }
}

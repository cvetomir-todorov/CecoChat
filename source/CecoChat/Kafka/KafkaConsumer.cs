﻿using System;
using System.Collections.Generic;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace CecoChat.Kafka
{
    public interface IKafkaConsumer<TKey, TValue> : IDisposable
    {
        void Initialize(IKafkaOptions options, IKafkaConsumerOptions consumerOptions, IDeserializer<TValue> valueDeserializer);

        void Subscribe(string topic);

        void Assign(string topic, PartitionRange partitions, ITopicPartitionFlyweight partitionFlyweight);

        bool TryConsume(CancellationToken ct, out ConsumeResult<TKey, TValue> consumeResult);

        void Commit(ConsumeResult<TKey, TValue> consumeResult, CancellationToken ct);
    }

    public sealed class KafkaConsumer<TKey, TValue> : IKafkaConsumer<TKey, TValue>
    {
        private readonly ILogger _logger;
        private PartitionRange _assignedPartitions;
        private IConsumer<TKey, TValue> _consumer;
        private string _id;

        public KafkaConsumer(ILogger<KafkaConsumer<TKey, TValue>> logger)
        {
            _logger = logger;
            _assignedPartitions = PartitionRange.Empty;
        }

        public void Dispose()
        {
            _consumer?.Close();
        }

        public void Initialize(IKafkaOptions options, IKafkaConsumerOptions consumerOptions, IDeserializer<TValue> valueDeserializer)
        {
            if (_consumer != null)
                throw new InvalidOperationException($"'{nameof(Initialize)}' already called.");

            ConsumerConfig configuration = new()
            {
                BootstrapServers = string.Join(separator: ',', options.BootstrapServers),
                GroupId = consumerOptions.ConsumerGroupID,
                AutoOffsetReset = consumerOptions.AutoOffsetReset,
                EnablePartitionEof = consumerOptions.EnablePartitionEof,
                AllowAutoCreateTopics = consumerOptions.AllowAutoCreateTopics,
                EnableAutoCommit = consumerOptions.EnableAutoCommit
            };

            _consumer = new ConsumerBuilder<TKey, TValue>(configuration)
                .SetValueDeserializer(valueDeserializer)
                .Build();
            _id = $"{KafkaConsumerIDGenerator.GetNextID()}@{consumerOptions.ConsumerGroupID}";
        }

        public void Subscribe(string topic)
        {
            EnsureInitialized();
            _consumer.Subscribe(topic);
            _logger.LogDebug("Consumer {0} subscribed to topic {1}.", _id, topic);
        }

        public void Assign(string topic, PartitionRange partitions, ITopicPartitionFlyweight partitionFlyweight)
        {
            EnsureInitialized();

            if (_assignedPartitions.Equals(partitions))
            {
                _logger.LogDebug("Consumer {0} already assigned partitions {1}.", _id, _assignedPartitions);
                return;
            }

            List<TopicPartition> topicPartitions = new(capacity: partitions.Length);
            for (int partition = partitions.Lower; partition <= partitions.Upper; ++partition)
            {
                TopicPartition topicPartition = partitionFlyweight.GetTopicPartition(topic, partition);
                topicPartitions.Add(topicPartition);
            }

            _consumer.Assign(topicPartitions);
            _assignedPartitions = partitions;
            _logger.LogDebug("Consumer {0} assigned partitions {1} from topic {2}.", _id, partitions, topic);
        }

        public bool TryConsume(CancellationToken ct, out ConsumeResult<TKey, TValue> consumeResult)
        {
            EnsureInitialized();

            consumeResult = Execute(() => _consumer.Consume(ct), ct);
            bool success = consumeResult != default;

            return success;
        }

        public void Commit(ConsumeResult<TKey, TValue> consumeResult, CancellationToken ct)
        {
            EnsureInitialized();

            bool success = Execute(() =>
            {
                _consumer.Commit(consumeResult);
                return true;
            }, ct);

            if (!success)
            {
                _logger.LogError("Consumer {0} failed to commit topic {1} partition {2} offset {3}.",
                    _id, consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);
            }
        }

        private void EnsureInitialized()
        {
            if (_consumer == null)
                throw new InvalidOperationException($"'{nameof(Initialize)}' needs to be called first.");
        }

        private TResult Execute<TResult>(Func<TResult> operation, CancellationToken ct)
        {
            try
            {
                return operation();
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
                _logger.LogError(exception, "Consumer {0} error.", _id);
            }

            return default;
        }

        private void HandleConsumerDisposal(Exception exception, CancellationToken ct)
        {
            if (!ct.IsCancellationRequested)
            {
                _logger.LogError(exception, "Consumer {0} was disposed without cancellation being requested.", _id);
            }
        }
    }

    /// <summary>
    /// Not inside the <see cref="KafkaConsumer{TKey,TValue}"/> class which uses it since it is generic.
    /// </summary>
    internal static class KafkaConsumerIDGenerator
    {
        private static int _nextIDCounter;
        public static int GetNextID() => Interlocked.Increment(ref _nextIDCounter);
    }
}

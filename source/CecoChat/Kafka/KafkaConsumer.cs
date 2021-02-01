using System;
using System.Collections.Generic;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace CecoChat.Kafka
{
    public interface IKafkaConsumer<TKey, TValue> : IDisposable
    {
        void Initialize(IKafkaOptions options, IDeserializer<TValue> valueDeserializer);

        void Subscribe(string topic);

        void Assign(string topic, int minPartition, int maxPartition, ITopicPartitionFlyweight partitionFlyweight);

        bool TryConsume(CancellationToken ct, out ConsumeResult<TKey, TValue> consumeResult);

        void Commit(ConsumeResult<TKey, TValue> consumeResult, CancellationToken ct);
    }

    public sealed class KafkaConsumer<TKey, TValue> : IKafkaConsumer<TKey, TValue>
    {
        private readonly ILogger _logger;
        private IConsumer<TKey, TValue> _consumer;

        public KafkaConsumer(ILogger<KafkaConsumer<TKey, TValue>> logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            _consumer?.Close();
        }

        public void Initialize(IKafkaOptions options, IDeserializer<TValue> valueDeserializer)
        {
            if (_consumer != null)
                throw new InvalidOperationException($"'{nameof(Initialize)}' already called.");

            ConsumerConfig configuration = new()
            {
                BootstrapServers = string.Join(separator: ',', options.BootstrapServers),
                GroupId = options.ConsumerGroupID,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnablePartitionEof = false,
                AllowAutoCreateTopics = false,
                EnableAutoCommit = false
            };

            _consumer = new ConsumerBuilder<TKey, TValue>(configuration)
                .SetValueDeserializer(valueDeserializer)
                .Build();
        }

        public void Subscribe(string topic)
        {
            EnsureInitialized();
            _consumer.Subscribe(topic);
            _logger.LogDebug("Consumer {0} subscribed to topic {1}.", _consumer.MemberId, topic);
        }

        public void Assign(string topic, int minPartition, int maxPartition, ITopicPartitionFlyweight partitionFlyweight)
        {
            EnsureInitialized();

            int count = maxPartition - minPartition + 1;
            List<TopicPartition> topicPartitions = new(capacity: count);
            for (int partition = minPartition; partition <= maxPartition; ++partition)
            {
                TopicPartition topicPartition = partitionFlyweight.GetTopicPartition(topic, partition);
                topicPartitions.Add(topicPartition);
            }

            _consumer.Assign(topicPartitions);
            _logger.LogDebug("Consumer {0} assigned partitions [{1}, {2}].", _consumer.MemberId, minPartition, maxPartition);
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
                    _consumer.MemberId, consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);
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
                _logger.LogError(exception, "Consumer {0} error.", _consumer.MemberId);
            }

            return default;
        }

        private void HandleConsumerDisposal(Exception exception, CancellationToken ct)
        {
            if (!ct.IsCancellationRequested)
            {
                _logger.LogError(exception, "Consumer {0} was disposed without cancellation being requested.", _consumer.MemberId);
            }
        }
    }
}

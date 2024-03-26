using System.Collections.Concurrent;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Common.Kafka;

public interface ITopicPartitionFlyweight
{
    void Add(string topic, int partitionCount);

    void AddOrUpdate(string topic, int partitionCount);

    TopicPartition GetTopicPartition(string topic, int partition);

    int GetTopicPartitionCount(string topic);
}

public sealed class TopicPartitionFlyweight : ITopicPartitionFlyweight
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, TopicPartition[]> _topicPartitionsMap;

    public TopicPartitionFlyweight(
        ILogger<TopicPartitionFlyweight> logger)
    {
        _logger = logger;
        _topicPartitionsMap = new();
    }

    public void Add(string topic, int partitionCount)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException($"{nameof(topic)} should be a non-empty non-whitespace string.");
        }
        if (partitionCount <= 0)
        {
            throw new ArgumentException($"{nameof(partitionCount)} should be greater than zero.");
        }

        Set(topic, partitionCount, strictlyAdd: true);
    }

    public void AddOrUpdate(string topic, int partitionCount)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException($"{nameof(topic)} should be a non-empty non-whitespace string.");
        }
        if (partitionCount <= 0)
        {
            throw new ArgumentException($"{nameof(partitionCount)} should be greater than zero.");
        }

        Set(topic, partitionCount, strictlyAdd: false);
    }

    private void Set(string topic, int partitionCount, bool strictlyAdd)
    {
        TopicPartition[] topicPartitions = new TopicPartition[partitionCount];

        for (int partition = 0; partition < partitionCount; ++partition)
        {
            topicPartitions[partition] = new TopicPartition(topic, partition);
        }

        if (strictlyAdd)
        {
            if (!_topicPartitionsMap.TryAdd(topic, topicPartitions))
            {
                throw new InvalidOperationException($"Topic {topic} is already added.");
            }
        }
        else
        {
            _topicPartitionsMap.AddOrUpdate(topic, topicPartitions, (_, _) => topicPartitions);
        }

        _logger.LogInformation("Set {Topic} topic {PartitionCount} partitions flyweight", topic, partitionCount);
    }

    public TopicPartition GetTopicPartition(string topic, int partition)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException($"{nameof(topic)} should be a non-empty non-whitespace string.");
        }
        if (partition < 0)
        {
            throw new ArgumentException($"{nameof(partition)} should be greater than zero.");
        }
        if (!_topicPartitionsMap.TryGetValue(topic, out TopicPartition[]? topicPartitions))
        {
            throw new InvalidOperationException($"Topic {topic} should be added first.");
        }
        if (partition >= topicPartitions.Length)
        {
            throw new InvalidOperationException($"Partition {partition} for topic {topic} should be within [0, {topicPartitions.Length - 1}].");
        }

        return topicPartitions[partition];
    }

    public int GetTopicPartitionCount(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException($"{nameof(topic)} should be a non-empty non-whitespace string.");
        }

        if (!_topicPartitionsMap.TryGetValue(topic, out TopicPartition[]? topicPartitions))
        {
            return 0;
        }

        return topicPartitions.Length;
    }
}

using System.Globalization;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Common.Kafka;

public sealed class KafkaTopicSpec
{
    public KafkaTopicSpec(string name, int partitionCount = 1, short replicationFactor = 2, int minInSyncReplicas = 0)
    {
        Name = name;
        PartitionCount = partitionCount;
        ReplicationFactor = replicationFactor;
        MinInSyncReplicas = minInSyncReplicas;
    }

    public string Name { get; init; }
    public int PartitionCount { get; init; }
    public short ReplicationFactor { get; init; }
    public int MinInSyncReplicas { get; init; }

    public override string ToString()
    {
        return $"[topic:{Name} partitions:{PartitionCount} replication-factor:{ReplicationFactor} min.insync.replicas:{MinInSyncReplicas}]";
    }
}

public interface IKafkaAdmin
{
    Task CreateTopics(IEnumerable<KafkaTopicSpec> topics);
}

public class KafkaAdmin : IKafkaAdmin
{
    private readonly ILogger _logger;
    private readonly IAdminClient _adminClient;

    public KafkaAdmin(
        ILogger<KafkaAdmin> logger,
        IOptions<KafkaOptions> kafkaOptions)
    {
        _logger = logger;

        AdminClientConfig config = new()
        {
            BootstrapServers = string.Join(separator: ',', kafkaOptions.Value.BootstrapServers)
        };

        _adminClient = new AdminClientBuilder(config).Build();
    }

    public async Task CreateTopics(IEnumerable<KafkaTopicSpec> topics)
    {
        Metadata metadata = _adminClient.GetMetadata(TimeSpan.FromSeconds(5));

        foreach (KafkaTopicSpec topic in topics)
        {
            if (!ContainsTopic(metadata, topic.Name))
            {
                await CreateTopic(topic);
            }
            else
            {
                _logger.LogInformation("Kafka topic {KafkaTopic} already exists, skip creating it", topic.Name);
            }
        }
    }

    private static bool ContainsTopic(Metadata metadata, string topic)
    {
        foreach (TopicMetadata topicMetadata in metadata.Topics)
        {
            if (string.Compare(topicMetadata.Topic, topic, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                return true;
            }
        }

        return false;
    }

    private async Task CreateTopic(KafkaTopicSpec topic)
    {
        TopicSpecification topicSpec = new();

        topicSpec.Name = topic.Name;
        topicSpec.NumPartitions = topic.PartitionCount;
        topicSpec.ReplicationFactor = topic.ReplicationFactor;

        if (topic.MinInSyncReplicas > 0)
        {
            topicSpec.Configs ??= new Dictionary<string, string>();
            topicSpec.Configs["min.insync.replicas"] = topic.MinInSyncReplicas.ToString(CultureInfo.InvariantCulture);
        }

        try
        {
            await _adminClient.CreateTopicsAsync(new[] { topicSpec });
            _logger.LogInformation("Kafka topic {KafkaTopic} created successfully", topic);
        }
        catch (KafkaException kafkaException)
        {
            _logger.LogError(kafkaException, "Failed to create Kafka topic {KafkaTopic}: {KafkaTopicError}", topic, kafkaException.Error);
        }
    }
}

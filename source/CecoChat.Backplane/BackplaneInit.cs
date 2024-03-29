using Common.AspNet.Init;
using Common.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CecoChat.Backplane;

public class BackplaneInit : InitStep
{
    private readonly ILogger _logger;
    private readonly IKafkaAdmin _kafkaAdmin;

    public BackplaneInit(
        ILogger<BackplaneInit> logger,
        IKafkaAdmin kafkaAdmin,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _logger = logger;
        _kafkaAdmin = kafkaAdmin;
    }

    protected override async Task<bool> DoExecute(CancellationToken ct)
    {
        KafkaTopicSpec[] topics =
        {
            new("messages-by-receiver", partitionCount: 12, replicationFactor: 2, minInSyncReplicas: 2),
            new("config-changes", partitionCount: 1, replicationFactor: 2, minInSyncReplicas: 2),
            new("health", partitionCount: 1, replicationFactor: 1)
        };

        await _kafkaAdmin.CreateTopics(topics);
        _logger.LogInformation("Ensured {TopicCount} Kafka topics exist", topics.Length);

        return true;
    }
}

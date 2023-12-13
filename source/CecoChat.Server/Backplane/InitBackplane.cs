using CecoChat.Kafka;
using Microsoft.Extensions.Hosting;

namespace CecoChat.Server.Backplane;

public class InitBackplane : IHostedService
{
    private readonly IKafkaAdmin _kafkaAdmin;

    public InitBackplane(IKafkaAdmin kafkaAdmin)
    {
        _kafkaAdmin = kafkaAdmin;
    }

    public Task StartAsync(CancellationToken _)
    {
        KafkaTopicSpec[] topics =
        {
            new("messages-by-receiver", partitionCount: 12, replicationFactor: 2, minInSyncReplicas: 2),
            new("config-changes", partitionCount: 12, replicationFactor: 2, minInSyncReplicas: 2),
            new("health", partitionCount: 1, replicationFactor: 1)
        };

        return _kafkaAdmin.CreateTopics(topics);
    }

    public Task StopAsync(CancellationToken _)
    {
        return Task.CompletedTask;
    }
}

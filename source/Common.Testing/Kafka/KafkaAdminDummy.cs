using Common.Kafka;

namespace Common.Testing.Kafka;

public sealed class KafkaAdminDummy : IKafkaAdmin
{
#pragma warning disable IDE0060 // Remove unused parameter
    public Task CreateTopics(IEnumerable<KafkaTopicSpec> topics)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        return Task.CompletedTask;
    }
}

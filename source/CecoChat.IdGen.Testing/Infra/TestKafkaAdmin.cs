using Common.Kafka;

namespace CecoChat.IdGen.Testing.Infra;

public sealed class TestKafkaAdmin : IKafkaAdmin
{
#pragma warning disable IDE0060 // Remove unused parameter
    public Task CreateTopics(IEnumerable<KafkaTopicSpec> topics)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        return Task.CompletedTask;
    }
}

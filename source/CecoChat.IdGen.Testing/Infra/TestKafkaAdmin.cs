using Common.Kafka;

namespace CecoChat.IdGen.Testing.Infra;

public class TestKafkaAdmin : IKafkaAdmin
{
    public Task CreateTopics(IEnumerable<KafkaTopicSpec> topics)
    {
        return Task.CompletedTask;
    }
}

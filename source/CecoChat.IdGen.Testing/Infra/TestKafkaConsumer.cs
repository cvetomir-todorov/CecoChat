using Common.Kafka;
using Confluent.Kafka;

namespace CecoChat.IdGen.Testing.Infra;

public class TestKafkaConsumer<TKey, TValue> : IKafkaConsumer<TKey, TValue>
{
    public void Dispose()
    { }

    public void Initialize(KafkaOptions options, KafkaConsumerOptions consumerOptions, IDeserializer<TValue> valueDeserializer)
    { }

    public void Subscribe(string topic)
    { }

    public void Assign(string topic, PartitionRange partitions, ITopicPartitionFlyweight partitionFlyweight)
    { }

    public void Consume(Action<ConsumeResult<TKey, TValue>> messageHandler, CancellationToken ct)
    { }
}

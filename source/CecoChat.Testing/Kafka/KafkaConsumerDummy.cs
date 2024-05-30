using Common.Kafka;
using Confluent.Kafka;

namespace CecoChat.Testing.Kafka;

public sealed class KafkaConsumerDummy<TKey, TValue> : IKafkaConsumer<TKey, TValue>
{
    public void Dispose()
    { }

#pragma warning disable IDE0060 // Remove unused parameter
    public void Initialize(KafkaOptions options, KafkaConsumerOptions consumerOptions, IDeserializer<TValue> valueDeserializer)
#pragma warning restore IDE0060 // Remove unused parameter
    { }

#pragma warning disable IDE0060 // Remove unused parameter
    public void Subscribe(string topic)
#pragma warning restore IDE0060 // Remove unused parameter
    { }

#pragma warning disable IDE0060 // Remove unused parameter
    public void Assign(string topic, PartitionRange partitions, ITopicPartitionFlyweight partitionFlyweight)
#pragma warning restore IDE0060 // Remove unused parameter
    { }

#pragma warning disable IDE0060 // Remove unused parameter
    public void Consume(Action<ConsumeResult<TKey, TValue>> messageHandler, CancellationToken ct)
#pragma warning restore IDE0060 // Remove unused parameter
    { }
}

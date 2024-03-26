namespace Common.Kafka;

/// <summary>
/// Not inside the <see cref="KafkaProducer{TKey,TValue}"/> class which uses it since it is generic.
/// </summary>
internal static class KafkaProducerIdGenerator
{
    private static int _nextIdCounter;

    public static int GetNextId()
    {
        return Interlocked.Increment(ref _nextIdCounter);
    }
}

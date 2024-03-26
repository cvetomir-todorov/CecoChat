namespace Common.Kafka;

/// <summary>
/// Not inside the <see cref="KafkaConsumer{TKey,TValue}"/> class which uses it since it is generic.
/// </summary>
internal static class KafkaConsumerIdGenerator
{
    private static int _nextIdCounter;

    public static int GetNextId()
    {
        return Interlocked.Increment(ref _nextIdCounter);
    }
}

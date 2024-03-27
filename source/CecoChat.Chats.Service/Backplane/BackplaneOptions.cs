using Common.Kafka;

namespace CecoChat.Chats.Service.Backplane;

public sealed class BackplaneOptions
{
    public KafkaOptions Kafka { get; init; } = new();

    public KafkaConsumerOptions HistoryConsumer { get; init; } = new();

    public KafkaConsumerOptions ReceiversConsumer { get; init; } = new();

    public KafkaConsumerOptions SendersConsumer { get; init; } = new();

    public string TopicMessagesByReceiver { get; init; } = string.Empty;
}

using CecoChat.Contracts.Backend;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Servers.Production
{
    public sealed class KafkaProducer : IBackendProducer
    {
        private readonly ILogger _logger;
        private readonly IKafkaOptions _options;
        private readonly IPartitionUtility _partitionUtility;
        private readonly IProducer<Null, Message> _producer;

        public KafkaProducer(ILogger<KafkaProducer> logger, IOptions<KafkaOptions> options, IPartitionUtility partitionUtility)
        {
            _logger = logger;
            _options = options.Value;
            _partitionUtility = partitionUtility;

            ProducerConfig configuration = new()
            {
                BootstrapServers = string.Join(separator: ',', _options.BootstrapServers),
                Acks = Acks.All,
                LingerMs = 1.0,
                MessageTimeoutMs = 300000,
                MessageSendMaxRetries = 8
            };

            _producer = new ProducerBuilder<Null, Message>(configuration)
                .SetValueSerializer(new KafkaProtobufSerializer<Message>())
                .Build();
        }

        public void Dispose()
        {
            _producer.Dispose();
        }

        public void ProduceMessage(Message message)
        {
            // TODO: use topic partition flyweight
            string topic = _options.MessagesTopic;
            int partition = _partitionUtility.ChoosePartition(message);
            TopicPartition topicPartition = new TopicPartition(topic, new Partition(partition));
            Message<Null, Message> kafkaMessage = new()
            {
                Value = message
            };

            _producer.Produce(topicPartition, kafkaMessage, DeliveryHandler);
        }

        private void DeliveryHandler(DeliveryReport<Null, Message> report)
        {
            Message message = report.Message.Value;

            if (report.Status != PersistenceStatus.Persisted)
            {
                _logger.LogError("Backend message {0} persistence status {1}.",
                    message.MessageID, report.Status);
            }
            if (report.Error.IsError)
            {
                _logger.LogError("Backend message {0} error '{1}'.",
                    message.MessageID, report.Error.Reason);
            }
            if (report.TopicPartitionOffsetError.Error.IsError)
            {
                _logger.LogError("Backend message {0} topic partition {1} error '{2}'.",
                    message.MessageID, report.TopicPartitionOffsetError.Partition, report.TopicPartitionOffsetError.Error.Reason);
            }
        }

        public void FlushPendingMessages()
        {
            _producer.Flush();
        }
    }
}

using System;
using CecoChat.Contracts.Backend;
using CecoChat.Server.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend.Production
{
    public sealed class KafkaProducer : IBackendProducer
    {
        private readonly ILogger _logger;
        private readonly IPartitionUtility _partitionUtility;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly IProducer<Null, BackendMessage> _producer;

        public KafkaProducer(
            ILogger<KafkaProducer> logger,
            IOptions<BackendOptions> options,
            IHostApplicationLifetime applicationLifetime,
            IPartitionUtility partitionUtility,
            ITopicPartitionFlyweight topicPartitionFlyweight)
        {
            _logger = logger;
            _partitionUtility = partitionUtility;
            _topicPartitionFlyweight = topicPartitionFlyweight;

            applicationLifetime.ApplicationStopping.Register(FlushPendingMessages);

            ProducerConfig configuration = new()
            {
                BootstrapServers = string.Join(separator: ',', options.Value.BootstrapServers),
                Acks = Acks.All,
                LingerMs = 1.0,
                MessageTimeoutMs = 300000,
                MessageSendMaxRetries = 8
            };

            _producer = new ProducerBuilder<Null, BackendMessage>(configuration)
                .SetValueSerializer(new KafkaBackendMessageSerializer())
                .Build();
        }

        public void Dispose()
        {
            _producer.Dispose();
        }

        private void FlushPendingMessages()
        {
            if (_producer == null)
                return;

            try
            {
                _logger.LogInformation("Flushing pending backend messages...");
                _producer.Flush();
                _logger.LogInformation("Flushing pending backend messages succeeded.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Flushing pending backend messages failed.");
            }
        }

        public void ProduceMessage(BackendMessage message)
        {
            int partition = _partitionUtility.ChoosePartition(message.ReceiverId);
            TopicPartition topicPartition = _topicPartitionFlyweight.GetMessagesTopicPartition(partition);
            Message<Null, BackendMessage> kafkaMessage = new()
            {
                Value = message
            };

            _producer.Produce(topicPartition, kafkaMessage, DeliveryHandler);
            _logger.LogTrace("Produced backend message {0}.", message);
        }

        private void DeliveryHandler(DeliveryReport<Null, BackendMessage> report)
        {
            BackendMessage message = report.Message.Value;

            if (report.Status != PersistenceStatus.Persisted)
            {
                _logger.LogError("Backend message {0} persistence status {1}.",
                    message.MessageId, report.Status);
            }
            if (report.Error.IsError)
            {
                _logger.LogError("Backend message {0} error '{1}'.",
                    message.MessageId, report.Error.Reason);
            }
            if (report.TopicPartitionOffsetError.Error.IsError)
            {
                _logger.LogError("Backend message {0} topic partition {1} error '{2}'.",
                    message.MessageId, report.TopicPartitionOffsetError.Partition, report.TopicPartitionOffsetError.Error.Reason);
            }
        }
    }
}

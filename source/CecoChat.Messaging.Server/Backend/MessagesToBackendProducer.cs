using System;
using CecoChat.Contracts.Backend;
using CecoChat.Kafka;
using CecoChat.Server.Backend;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IMessagesToBackendProducer : IDisposable
    {
        int PartitionCount { get; set; }

        void ProduceMessage(BackendMessage message);
    }

    public sealed class MessagesToBackendProducer : IMessagesToBackendProducer
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IPartitionUtility _partitionUtility;
        private readonly ITopicPartitionFlyweight _partitionFlyweight;
        private readonly IProducer<Null, BackendMessage> _producer;

        public MessagesToBackendProducer(
            ILogger<MessagesToBackendProducer> logger,
            IOptions<BackendOptions> backendOptions,
            IHostApplicationLifetime applicationLifetime,
            IPartitionUtility partitionUtility,
            ITopicPartitionFlyweight partitionFlyweight)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _partitionUtility = partitionUtility;
            _partitionFlyweight = partitionFlyweight;

            applicationLifetime.ApplicationStopping.Register(FlushPendingMessages);

            ProducerConfig configuration = new()
            {
                BootstrapServers = string.Join(separator: ',', _backendOptions.Kafka.BootstrapServers),
                Acks = _backendOptions.MessagesToBackendProducer.Acks,
                LingerMs = _backendOptions.MessagesToBackendProducer.LingerMs,
                MessageTimeoutMs = _backendOptions.MessagesToBackendProducer.MessageTimeoutMs,
                MessageSendMaxRetries = _backendOptions.MessagesToBackendProducer.MessageSendMaxRetries
            };

            _producer = new ProducerBuilder<Null, BackendMessage>(configuration)
                .SetValueSerializer(new BackendMessageSerializer())
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

        public int PartitionCount { get; set; }

        public void ProduceMessage(BackendMessage message)
        {
            int partition = _partitionUtility.ChoosePartition(message.TargetId, PartitionCount);
            TopicPartition topicPartition = _partitionFlyweight.GetTopicPartition(_backendOptions.MessagesTopicName, partition);
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

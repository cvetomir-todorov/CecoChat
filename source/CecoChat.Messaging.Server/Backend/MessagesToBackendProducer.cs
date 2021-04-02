using System;
using CecoChat.Contracts.Backend;
using CecoChat.DependencyInjection;
using CecoChat.Kafka;
using CecoChat.Server.Backend;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
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
        private readonly IBackendOptions _backendOptions;
        private readonly IPartitionUtility _partitionUtility;
        private readonly ITopicPartitionFlyweight _partitionFlyweight;
        private readonly IKafkaProducer<Null, BackendMessage> _producer;

        public MessagesToBackendProducer(
            IOptions<BackendOptions> backendOptions,
            IHostApplicationLifetime applicationLifetime,
            IPartitionUtility partitionUtility,
            ITopicPartitionFlyweight partitionFlyweight,
            IFactory<IKafkaProducer<Null, BackendMessage>> producerFactory)
        {
            _backendOptions = backendOptions.Value;
            _partitionUtility = partitionUtility;
            _partitionFlyweight = partitionFlyweight;

            _producer = producerFactory.Create();
            _producer.Initialize(_backendOptions.Kafka, _backendOptions.MessagesToBackendProducer, new BackendMessageSerializer());

            applicationLifetime.ApplicationStopping.Register(_producer.FlushPendingMessages);
        }

        public void Dispose()
        {
            _producer.Dispose();
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

            _producer.Produce(kafkaMessage, topicPartition);
        }
    }
}

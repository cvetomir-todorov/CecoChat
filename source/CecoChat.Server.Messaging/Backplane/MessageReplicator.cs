using System;
using System.Threading;
using CecoChat.Contracts.Backplane;
using CecoChat.Kafka;
using CecoChat.Server.Backplane;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.Backplane
{
    /// <summary>
    /// Consumes messages from topic which partitions them by receiver ID.
    /// Produces replicas into a topic which partitions them by sender ID.
    /// </summary>
    public interface IMessageReplicator : IDisposable
    {
        void Prepare(PartitionRange partitions);

        void Start(CancellationToken ct);

        int PartitionCount { get; set; }

        string ConsumerID { get; }
    }

    public sealed class MessageReplicator : IMessageReplicator
    {
        private readonly ILogger _logger;
        private readonly BackplaneOptions _backplaneOptions;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly IPartitionUtility _partitionUtility;
        private readonly ITopicPartitionFlyweight _partitionFlyweight;
        private readonly IKafkaConsumer<Null, BackplaneMessage> _consumer;
        private readonly IKafkaProducer<Null, BackplaneMessage> _producer;
        private bool _isInitialized;
        private readonly object _initializationLock;

        public MessageReplicator(
            ILogger<MessageReplicator> logger,
            IOptions<BackplaneOptions> backplaneOptions,
            IHostApplicationLifetime applicationLifetime,
            IPartitionUtility partitionUtility,
            ITopicPartitionFlyweight partitionFlyweight,
            IFactory<IKafkaConsumer<Null, BackplaneMessage>> consumerFactory,
            IFactory<IKafkaProducer<Null, BackplaneMessage>> producerFactory)
        {
            _logger = logger;
            _backplaneOptions = backplaneOptions.Value;
            _applicationLifetime = applicationLifetime;
            _partitionUtility = partitionUtility;
            _partitionFlyweight = partitionFlyweight;
            _consumer = consumerFactory.Create();
            _producer = producerFactory.Create();

            _initializationLock = new object();
        }

        public void Dispose()
        {
            _consumer.Dispose();
            _producer.Dispose();
        }

        public void Prepare(PartitionRange partitions)
        {
            lock (_initializationLock)
            {
                if (!_isInitialized)
                {
                    _consumer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.ReplicatingConsumer, new BackplaneMessageDeserializer());
                    _producer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.ReplicatingProducer, new BackplaneMessageSerializer());
                    _applicationLifetime.ApplicationStopping.Register(_producer.FlushPendingMessages);
                    _isInitialized = true;
                }
            }

            _consumer.Assign(_backplaneOptions.TopicMessagesByReceiver, partitions, _partitionFlyweight);
        }

        public void Start(CancellationToken ct)
        {
            _logger.LogInformation("Start replicating messages from topic {0} to topic {1}.", _backplaneOptions.TopicMessagesByReceiver, _backplaneOptions.TopicMessagesBySender);

            while (!ct.IsCancellationRequested)
            {
                _consumer.Consume(consumeResult =>
                {
                    BackplaneMessage message = consumeResult.Message.Value;
                    int partition = _partitionUtility.ChoosePartition(message.SenderId, PartitionCount);
                    TopicPartition topicPartition = _partitionFlyweight.GetTopicPartition(_backplaneOptions.TopicMessagesBySender, partition);
                    Message<Null, BackplaneMessage> kafkaMessage = new() {Value = message};
                    // the producer built-in delivery handler will log by default when messages are not delivered 
                    _producer.Produce(kafkaMessage, topicPartition);
                }, ct);
            }

            _logger.LogInformation("Stopped replicating messages from topic {0} to topic {1}.", _backplaneOptions.TopicMessagesByReceiver, _backplaneOptions.TopicMessagesBySender);
        }

        public int PartitionCount { get; set; }

        public string ConsumerID => _backplaneOptions.ReplicatingConsumer.ConsumerGroupID;
    }
}
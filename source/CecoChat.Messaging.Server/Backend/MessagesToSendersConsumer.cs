using System.Threading;
using CecoChat.Contracts.Backend;
using CecoChat.Kafka;
using CecoChat.Server.Backend;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IMessagesToSendersConsumer : IBackendConsumer
    {}

    public sealed class MessagesToSendersConsumer : IMessagesToSendersConsumer
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly ITopicPartitionFlyweight _partitionFlyweight;
        private readonly IKafkaConsumer<Null, BackendMessage> _consumer;
        private readonly IMessagesToBackendProducer _messagesToBackendProducer;
        private bool _isInitialized;
        private readonly object _initializationLock;

        public MessagesToSendersConsumer(
            ILogger<MessagesToReceiversConsumer> logger,
            IOptions<BackendOptions> backendOptions,
            ITopicPartitionFlyweight partitionFlyweight,
            IFactory<IKafkaConsumer<Null, BackendMessage>> consumerFactory,
            IMessagesToBackendProducer messagesToBackendProducer)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _partitionFlyweight = partitionFlyweight;
            _consumer = consumerFactory.Create();
            _messagesToBackendProducer = messagesToBackendProducer;

            _initializationLock = new object();
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }

        public void Prepare(PartitionRange partitions)
        {
            lock (_initializationLock)
            {
                if (!_isInitialized)
                {
                    _consumer.Initialize(_backendOptions.Kafka, _backendOptions.MessagesToSendersConsumer, new BackendMessageDeserializer());
                    _isInitialized = true;
                }
            }

            _consumer.Assign(_backendOptions.MessagesTopicName, partitions, _partitionFlyweight);
        }

        public void Start(CancellationToken ct)
        {
            _logger.LogInformation("Start sending messages to senders.");

            while (!ct.IsCancellationRequested)
            {
                if (_consumer.TryConsume(ct, out ConsumeResult<Null, BackendMessage> consumeResult))
                {
                    BackendMessage message = consumeResult.Message.Value;
                    if (message.TargetId == message.ReceiverId)
                    {
                        message.TargetId = message.SenderId;
                        _messagesToBackendProducer.ProduceMessage(message, sendAck: false);
                        _logger.LogTrace("Duplicated for sender message {0}.", message);
                    }
                    _consumer.Commit(consumeResult, ct);
                }
            }

            _logger.LogInformation("Stopped sending messages to senders.");
        }
    }
}

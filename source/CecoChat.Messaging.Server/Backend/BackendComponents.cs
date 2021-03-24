using CecoChat.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IBackendComponents
    {
        IMessagesToReceiversConsumer MessagesToReceiversConsumer { get; }

        IMessagesToSendersConsumer MessagesToSendersConsumer { get; }

        void ConfigurePartitioning(int partitionCount, PartitionRange partitions);
    }

    public sealed class BackendComponents : IBackendComponents
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly IMessagesToBackendProducer _messagesToBackendProducer;
        private readonly IMessagesToReceiversConsumer _messagesToReceiversConsumer;
        private readonly IMessagesToSendersConsumer _messagesToSendersConsumer;

        public BackendComponents(
            ILogger<BackendComponents> logger,
            IOptions<BackendOptions> backendOptions,
            ITopicPartitionFlyweight topicPartitionFlyweight,
            IMessagesToBackendProducer messagesToBackendProducer,
            IMessagesToReceiversConsumer messagesToReceiversConsumer,
            IMessagesToSendersConsumer messagesToSendersConsumer)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _topicPartitionFlyweight = topicPartitionFlyweight;
            _messagesToBackendProducer = messagesToBackendProducer;
            _messagesToReceiversConsumer = messagesToReceiversConsumer;
            _messagesToSendersConsumer = messagesToSendersConsumer;
        }

        public IMessagesToReceiversConsumer MessagesToReceiversConsumer => _messagesToReceiversConsumer;

        public IMessagesToSendersConsumer MessagesToSendersConsumer => _messagesToSendersConsumer;

        public void ConfigurePartitioning(int partitionCount, PartitionRange partitions)
        {
            int currentPartitionCount = _topicPartitionFlyweight.GetTopicPartitionCount(_backendOptions.MessagesTopicName);
            if (currentPartitionCount < partitionCount)
            {
                _topicPartitionFlyweight.AddOrUpdate(_backendOptions.MessagesTopicName, partitionCount);
                _logger.LogInformation("Increase cached partitions for topic {0} from {1} to {2}.",
                    _backendOptions.MessagesTopicName, currentPartitionCount, partitionCount);
            }

            _messagesToBackendProducer.PartitionCount = partitionCount;
            _messagesToReceiversConsumer.Prepare(partitions);
            _messagesToSendersConsumer.Prepare(partitions);
            _logger.LogInformation("Prepared backend components for topic {0} to use partitions {1}.",
                _backendOptions.MessagesTopicName, partitions);
        }
    }
}

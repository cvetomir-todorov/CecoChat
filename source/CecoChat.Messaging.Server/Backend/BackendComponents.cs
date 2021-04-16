using System.Collections.Generic;
using CecoChat.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IBackendComponents
    {
        IEnumerable<IBackendConsumer> BackendConsumers { get; }

        void ConfigurePartitioning(int partitionCount, PartitionRange partitions);
    }

    public sealed class BackendComponents : IBackendComponents
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly IMessagesToBackendProducer _messagesToBackendProducer;
        private readonly List<IBackendConsumer> _backendConsumers;

        public BackendComponents(
            ILogger<BackendComponents> logger,
            IOptions<BackendOptions> backendOptions,
            ITopicPartitionFlyweight topicPartitionFlyweight,
            IMessagesToBackendProducer messagesToBackendProducer,
            IEnumerable<IBackendConsumer> backendConsumers)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _topicPartitionFlyweight = topicPartitionFlyweight;
            _messagesToBackendProducer = messagesToBackendProducer;
            _backendConsumers = new List<IBackendConsumer>(backendConsumers);
        }

        public IEnumerable<IBackendConsumer> BackendConsumers => _backendConsumers;

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
            foreach (IBackendConsumer backendConsumer in _backendConsumers)
            {
                backendConsumer.Prepare(partitions);
            }

            _logger.LogInformation("Prepared backend components for topic {0} to use partitions {1}.",
                _backendOptions.MessagesTopicName, partitions);
        }
    }
}

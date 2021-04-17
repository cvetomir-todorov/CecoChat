using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public interface IBackendComponents
    {
        void ConfigurePartitioning(int partitionCount, PartitionRange partitions);

        void StartConsumption(CancellationToken ct);
    }

    public sealed class BackendComponents : IBackendComponents
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly ISendProducer _sendProducer;
        private readonly IReceiversConsumer _receiversConsumer;

        public BackendComponents(
            ILogger<BackendComponents> logger,
            IOptions<BackendOptions> backendOptions,
            ITopicPartitionFlyweight topicPartitionFlyweight,
            ISendProducer sendProducer,
            IReceiversConsumer receiversConsumer)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _topicPartitionFlyweight = topicPartitionFlyweight;
            _sendProducer = sendProducer;
            _receiversConsumer = receiversConsumer;
        }

        public void ConfigurePartitioning(int partitionCount, PartitionRange partitions)
        {
            int currentPartitionCount = _topicPartitionFlyweight.GetTopicPartitionCount(_backendOptions.MessagesTopicName);
            if (currentPartitionCount < partitionCount)
            {
                _topicPartitionFlyweight.AddOrUpdate(_backendOptions.MessagesTopicName, partitionCount);
                _logger.LogInformation("Increase cached partitions for topic {0} from {1} to {2}.",
                    _backendOptions.MessagesTopicName, currentPartitionCount, partitionCount);
            }

            _sendProducer.PartitionCount = partitionCount;
            _receiversConsumer.Prepare(partitions);

            _logger.LogInformation("Prepared backend components for topic {0} to use partitions {1}.",
                _backendOptions.MessagesTopicName, partitions);
        }

        public void StartConsumption(CancellationToken ct)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    _receiversConsumer.Start(ct);
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Failure in {0} consumer.", _receiversConsumer.ConsumerID);
                }
            }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }
    }
}

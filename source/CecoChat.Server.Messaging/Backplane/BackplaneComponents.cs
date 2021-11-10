using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.Backplane
{
    public interface IBackplaneComponents : IDisposable
    {
        void ConfigurePartitioning(int partitionCount, PartitionRange partitions);

        void StartConsumption(CancellationToken ct);
    }

    public sealed class BackplaneComponents : IBackplaneComponents
    {
        private readonly ILogger _logger;
        private readonly BackplaneOptions _backplaneOptions;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly ISendersProducer _sendersProducer;
        private readonly IReceiversConsumer _receiversConsumer;

        public BackplaneComponents(
            ILogger<BackplaneComponents> logger,
            IOptions<BackplaneOptions> backplaneOptions,
            ITopicPartitionFlyweight topicPartitionFlyweight,
            ISendersProducer sendersProducer,
            IReceiversConsumer receiversConsumer)
        {
            _logger = logger;
            _backplaneOptions = backplaneOptions.Value;
            _topicPartitionFlyweight = topicPartitionFlyweight;
            _sendersProducer = sendersProducer;
            _receiversConsumer = receiversConsumer;
        }

        public void Dispose()
        {
            _sendersProducer.Dispose();
            _receiversConsumer.Dispose();
        }

        public void ConfigurePartitioning(int partitionCount, PartitionRange partitions)
        {
            int currentPartitionCount = _topicPartitionFlyweight.GetTopicPartitionCount(_backplaneOptions.MessagesTopicName);
            if (currentPartitionCount < partitionCount)
            {
                _topicPartitionFlyweight.AddOrUpdate(_backplaneOptions.MessagesTopicName, partitionCount);
                _logger.LogInformation("Increase cached partitions for topic {0} from {1} to {2}.",
                    _backplaneOptions.MessagesTopicName, currentPartitionCount, partitionCount);
            }

            _sendersProducer.PartitionCount = partitionCount;
            _receiversConsumer.Prepare(partitions);

            _logger.LogInformation("Prepared backplane components for topic {0} to use partitions {1}.",
                _backplaneOptions.MessagesTopicName, partitions);
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

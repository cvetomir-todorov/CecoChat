using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Configuration.Partitioning;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Backend;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Initialization
{
    public sealed class BackendHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IPartitioningConfiguration _partitioningConfiguration;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly IMessagesToBackendProducer _messagesToBackendProducer;
        private readonly IMessagesToReceiversConsumer _messagesToReceiversConsumer;
        private readonly IMessagesToSendersConsumer _messagesToSendersConsumer;

        public BackendHostedService(
            ILogger<BackendHostedService> logger,
            IOptions<BackendOptions> backendOptions,
            IPartitioningConfiguration partitioningConfiguration,
            ITopicPartitionFlyweight topicPartitionFlyweight,
            IMessagesToBackendProducer messagesToBackendProducer,
            IMessagesToReceiversConsumer messagesToReceiversConsumer,
            IMessagesToSendersConsumer messagesToSendersConsumer)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _partitioningConfiguration = partitioningConfiguration;
            _topicPartitionFlyweight = topicPartitionFlyweight;
            _messagesToBackendProducer = messagesToBackendProducer;
            _messagesToReceiversConsumer = messagesToReceiversConsumer;
            _messagesToSendersConsumer = messagesToSendersConsumer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            int partitionCount = _partitioningConfiguration.PartitionCount;
            PartitionRange partitions = _partitioningConfiguration.GetServerPartitions(_backendOptions.ServerID);

            ConfigureBackend(partitionCount, partitions);
            StartBackendConsumer(_messagesToReceiversConsumer, "messages to receivers", cancellationToken);
            StartBackendConsumer(_messagesToSendersConsumer, "messages to senders", cancellationToken);

            return Task.CompletedTask;
        }

        private void ConfigureBackend(int partitionCount, PartitionRange partitions)
        {
            _topicPartitionFlyweight.Add(_backendOptions.MessagesTopicName, partitionCount);
            _messagesToBackendProducer.PartitionCount = partitionCount;
            _messagesToReceiversConsumer.Prepare(partitions);
            _messagesToSendersConsumer.Prepare(partitions);
        }

        private void StartBackendConsumer(IBackendConsumer consumer, string consumerID, CancellationToken ct)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    consumer.Start(ct);
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Failure in {0} consumer.", consumerID);
                }
            }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

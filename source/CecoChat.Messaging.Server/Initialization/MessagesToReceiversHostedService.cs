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
    public sealed class MessagesToReceiversHostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IPartitioningConfiguration _partitioningConfiguration;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly IBackendProducer _backendProducer;
        private readonly IBackendConsumer _backendConsumer;

        public MessagesToReceiversHostedService(
            ILogger<MessagesToReceiversHostedService> logger,
            IOptions<BackendOptions> backendOptions,
            IPartitioningConfiguration partitioningConfiguration,
            ITopicPartitionFlyweight topicPartitionFlyweight,
            IBackendProducer backendProducer,
            IBackendConsumer backendConsumer)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _partitioningConfiguration = partitioningConfiguration;
            _topicPartitionFlyweight = topicPartitionFlyweight;
            _backendProducer = backendProducer;
            _backendConsumer = backendConsumer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            int partitionCount = _partitioningConfiguration.PartitionCount;
            PartitionRange partitions = _partitioningConfiguration.GetServerPartitions(_backendOptions.ServerID);

            ConfigureBackend(partitionCount, partitions);
            StartBackendConsumer(cancellationToken);

            return Task.CompletedTask;
        }

        private void ConfigureBackend(int partitionCount, PartitionRange partitions)
        {
            _topicPartitionFlyweight.Add(_backendOptions.MessagesTopicName, partitionCount);
            _backendConsumer.Prepare(partitions);
            _backendProducer.PartitionCount = partitionCount;
        }

        private void StartBackendConsumer(CancellationToken ct)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    _backendConsumer.Start(ct);
                }
                catch (Exception exception)
                {
                    _logger.LogCritical(exception, "Failure in send messages to receivers hosted service.");
                }
            }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);

            _logger.LogInformation("Started send messages to receivers hosted service.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

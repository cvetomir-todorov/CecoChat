using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Data.Configuration.Messaging;
using CecoChat.Events;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Backend;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Initialization
{
    public sealed class MessagesToReceiversHostedService : IHostedService, ISubscriber<PartitionsChangedEventData>
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IMessagingConfiguration _messagingConfiguration;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly IBackendProducer _backendProducer;
        private readonly IBackendConsumer _backendConsumer;
        private readonly IEvent<PartitionsChangedEventData> _partitionsChanged;
        private readonly Guid _partitionsChangedToken;

        public MessagesToReceiversHostedService(
            ILogger<MessagesToReceiversHostedService> logger,
            IOptions<BackendOptions> backendOptions,
            IMessagingConfiguration messagingConfiguration,
            ITopicPartitionFlyweight topicPartitionFlyweight,
            IBackendProducer backendProducer,
            IBackendConsumer backendConsumer,
            IEvent<PartitionsChangedEventData> partitionsChanged)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _messagingConfiguration = messagingConfiguration;
            _topicPartitionFlyweight = topicPartitionFlyweight;
            _backendProducer = backendProducer;
            _backendConsumer = backendConsumer;
            _partitionsChanged = partitionsChanged;

            _partitionsChangedToken = _partitionsChanged.Subscribe(this);
        }

        public Task StartAsync(CancellationToken ct)
        {
            int partitionCount = _messagingConfiguration.PartitionCount;
            PartitionRange partitions = _messagingConfiguration.GetServerPartitions(_backendOptions.ServerID);

            if (ValidatePartitionConfiguration(partitionCount, partitions))
            {
                ConfigureBackend(partitionCount, partitions);
                StartBackendConsumer(ct);
            }

            return Task.CompletedTask;
        }

        public ValueTask Handle(PartitionsChangedEventData eventData)
        {
            int partitionCount = eventData.PartitionCount;
            PartitionRange partitions = eventData.Partitions;

            if (ValidatePartitionConfiguration(partitionCount, partitions))
            {
                DisconnectClients(partitions);
                ConfigureBackend(partitionCount, partitions);
            }

            return ValueTask.CompletedTask;
        }

        private bool ValidatePartitionConfiguration(int partitionCount, PartitionRange partitions)
        {
            bool isValid = partitions.Upper < partitionCount;
            if (!isValid)
            {
                _logger.LogCritical("Rejecting invalid partition configuration - assigned partitions {0} are outside partition count {1}.",
                    partitions, partitionCount);
            }

            return isValid;
        }

        private void ConfigureBackend(int partitionCount, PartitionRange partitions)
        {
            int currentPartitionCount = _topicPartitionFlyweight.GetTopicPartitionCount(_backendOptions.MessagesTopicName);
            if (currentPartitionCount < partitionCount)
            {
                _topicPartitionFlyweight.AddOrUpdate(_backendOptions.MessagesTopicName, partitionCount);
            }

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

        private void DisconnectClients(PartitionRange partitions)
        {
            // TODO: send a message to the clients connected to the server who should move to other server
        }

        public Task StopAsync(CancellationToken ct)
        {
            _partitionsChanged.Unsubscribe(_partitionsChangedToken);
            return Task.CompletedTask;
        }
    }
}

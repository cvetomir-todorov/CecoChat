using System;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.Client;
using CecoChat.Data.Configuration.Partitioning;
using CecoChat.Events;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Backend;
using CecoChat.Messaging.Server.Clients;
using CecoChat.Server.Backend;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Initialization
{
    public sealed class MessagesToReceiversHostedService : IHostedService, ISubscriber<PartitionsChangedEventData>
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IPartitioningConfiguration _partitioningConfiguration;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly IPartitionUtility _partitionUtility;
        private readonly IBackendProducer _backendProducer;
        private readonly IBackendConsumer _backendConsumer;
        private readonly IClientContainer _clientContainer;
        private readonly IEvent<PartitionsChangedEventData> _partitionsChanged;
        private readonly Guid _partitionsChangedToken;

        public MessagesToReceiversHostedService(
            ILogger<MessagesToReceiversHostedService> logger,
            IOptions<BackendOptions> backendOptions,
            IPartitioningConfiguration partitioningConfiguration,
            ITopicPartitionFlyweight topicPartitionFlyweight,
            IPartitionUtility partitionUtility,
            IBackendProducer backendProducer,
            IBackendConsumer backendConsumer,
            IClientContainer clientContainer,
            IEvent<PartitionsChangedEventData> partitionsChanged)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _partitioningConfiguration = partitioningConfiguration;
            _topicPartitionFlyweight = topicPartitionFlyweight;
            _partitionUtility = partitionUtility;
            _backendProducer = backendProducer;
            _backendConsumer = backendConsumer;
            _clientContainer = clientContainer;
            _partitionsChanged = partitionsChanged;

            _partitionsChangedToken = _partitionsChanged.Subscribe(this);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            int partitionCount = _partitioningConfiguration.PartitionCount;
            PartitionRange partitions = _partitioningConfiguration.GetServerPartitions(_backendOptions.ServerID);

            if (ValidatePartitionConfiguration(partitionCount, partitions))
            {
                ConfigureBackend(partitionCount, partitions);
                StartBackendConsumer(cancellationToken);
            }

            return Task.CompletedTask;
        }

        public ValueTask Handle(PartitionsChangedEventData eventData)
        {
            int partitionCount = eventData.PartitionCount;
            PartitionRange partitions = eventData.Partitions;

            if (ValidatePartitionConfiguration(partitionCount, partitions))
            {
                DisconnectClients(partitionCount, partitions);
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

        private void DisconnectClients(int partitionCount, PartitionRange partitions)
        {
            foreach (var pair in _clientContainer.EnumerateAllClients())
            {
                long userID = pair.Key;
                var clients = pair.Value;

                int userPartition = _partitionUtility.ChoosePartition(userID, partitionCount);
                if (!partitions.Contains(userPartition))
                {
                    foreach (IStreamer<ListenResponse> client in clients)
                    {
                        ClientMessage disconnectMessage = new() {Type = ClientMessageType.Disconnect};
                        ListenResponse response = new() {Message = disconnectMessage};
                        client.AddMessage(response);
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _partitionsChanged.Unsubscribe(_partitionsChangedToken);
            return Task.CompletedTask;
        }
    }
}

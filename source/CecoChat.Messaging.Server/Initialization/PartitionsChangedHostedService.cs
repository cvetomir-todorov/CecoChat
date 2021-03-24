using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Initialization
{
    public sealed class PartitionsChangedHostedService : IHostedService, ISubscriber<PartitionsChangedEventData>
    {
        private readonly IBackendOptions _backendOptions;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly IPartitionUtility _partitionUtility;
        private readonly IMessagesToBackendProducer _messagesToBackendProducer;
        private readonly IMessagesToReceiversConsumer _messagesToReceiversConsumer;
        private readonly IMessagesToSendersConsumer _messagesToSendersConsumer;
        private readonly IClientContainer _clientContainer;
        private readonly IEvent<PartitionsChangedEventData> _partitionsChanged;
        private readonly Guid _partitionsChangedToken;

        public PartitionsChangedHostedService(
            IOptions<BackendOptions> backendOptions,
            ITopicPartitionFlyweight topicPartitionFlyweight,
            IPartitionUtility partitionUtility,
            IMessagesToBackendProducer messagesToBackendProducer,
            IMessagesToReceiversConsumer messagesToReceiversConsumer,
            IMessagesToSendersConsumer messagesToSendersConsumer,
            IClientContainer clientContainer,
            IEvent<PartitionsChangedEventData> partitionsChanged)
        {
            _backendOptions = backendOptions.Value;
            _topicPartitionFlyweight = topicPartitionFlyweight;
            _partitionUtility = partitionUtility;
            _messagesToBackendProducer = messagesToBackendProducer;
            _messagesToReceiversConsumer = messagesToReceiversConsumer;
            _messagesToSendersConsumer = messagesToSendersConsumer;
            _clientContainer = clientContainer;
            _partitionsChanged = partitionsChanged;

            _partitionsChangedToken = _partitionsChanged.Subscribe(this);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _partitionsChanged.Unsubscribe(_partitionsChangedToken);
            return Task.CompletedTask;
        }

        public ValueTask Handle(PartitionsChangedEventData eventData)
        {
            int partitionCount = eventData.PartitionCount;
            PartitionRange partitions = eventData.Partitions;

            DisconnectClients(partitionCount, partitions);
            ConfigureBackend(partitionCount, partitions);

            return ValueTask.CompletedTask;
        }

        private void DisconnectClients(int partitionCount, PartitionRange partitions)
        {
            ClientMessage disconnectMessage = new() {Type = ClientMessageType.Disconnect};
            ListenResponse response = new() {Message = disconnectMessage};

            foreach (var pair in _clientContainer.EnumerateAllClients())
            {
                long userID = pair.Key;
                IEnumerable<IStreamer<ListenResponse>> clients = pair.Value;

                int userPartition = _partitionUtility.ChoosePartition(userID, partitionCount);
                if (!partitions.Contains(userPartition))
                {
                    foreach (IStreamer<ListenResponse> client in clients)
                    {
                        client.EnqueueMessage(response);
                    }
                }
            }
        }

        private void ConfigureBackend(int partitionCount, PartitionRange partitions)
        {
            int currentPartitionCount = _topicPartitionFlyweight.GetTopicPartitionCount(_backendOptions.MessagesTopicName);
            if (currentPartitionCount < partitionCount)
            {
                _topicPartitionFlyweight.AddOrUpdate(_backendOptions.MessagesTopicName, partitionCount);
            }

            _messagesToBackendProducer.PartitionCount = partitionCount;
            _messagesToReceiversConsumer.Prepare(partitions);
            _messagesToSendersConsumer.Prepare(partitions);
        }
    }
}

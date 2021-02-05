using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;
using CecoChat.Data.Configuration.Messaging;
using CecoChat.DependencyInjection;
using CecoChat.Events;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Clients;
using CecoChat.Server;
using CecoChat.Server.Backend;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public sealed class MessagesToReceiversConsumer : IBackendConsumer, ISubscriber<PartitionsChangedEventData>
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IMessagingConfiguration _messagingConfiguration;
        private readonly IEvent<PartitionsChangedEventData> _partitionsChanged;
        private readonly ITopicPartitionFlyweight _partitionFlyweight;
        private readonly IKafkaConsumer<Null, BackendMessage> _consumer;
        private readonly IClientContainer _clientContainer;
        private readonly IClientBackendMapper _mapper;

        private readonly Guid _partitionsChangedToken;

        public MessagesToReceiversConsumer(
            ILogger<MessagesToReceiversConsumer> logger,
            IOptions<BackendOptions> backendOptions,
            IMessagingConfiguration messagingConfiguration,
            IEvent<PartitionsChangedEventData> partitionsChanged,
            ITopicPartitionFlyweight partitionFlyweight,
            IFactory<IKafkaConsumer<Null, BackendMessage>> consumerFactory,
            IClientContainer clientContainer,
            IClientBackendMapper mapper)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _messagingConfiguration = messagingConfiguration;
            _partitionsChanged = partitionsChanged;
            _partitionFlyweight = partitionFlyweight;
            _consumer = consumerFactory.Create();
            _clientContainer = clientContainer;
            _mapper = mapper;

            _partitionsChangedToken = _partitionsChanged.Subscribe(this);
        }

        public void Dispose()
        {
            _partitionsChanged.Unsubscribe(_partitionsChangedToken);
            _consumer.Dispose();
        }

        public void Prepare()
        {
            _consumer.Initialize(_backendOptions.Kafka, new BackendMessageDeserializer());
            PartitionRange partitions = _messagingConfiguration.GetServerPartitions(_backendOptions.ServerID);
            _consumer.Assign(_backendOptions.MessagesTopicName, partitions, _partitionFlyweight);
        }

        public void Start(CancellationToken ct)
        {
            _logger.LogInformation("Start sending messages to receivers.");

            while (!ct.IsCancellationRequested)
            {
                if (_consumer.TryConsume(ct, out ConsumeResult<Null, BackendMessage> consumeResult))
                {
                    ProcessMessage(consumeResult.Message.Value);
                    _consumer.Commit(consumeResult, ct);
                }
            }

            _logger.LogInformation("Stopped sending messages to receivers.");
        }

        private void ProcessMessage(BackendMessage backendMessage)
        {
            IEnumerable<IStreamer<ListenResponse>> clients = _clientContainer.GetClients(backendMessage.ReceiverId);

            ClientMessage clientMessage = _mapper.MapBackendToClientMessage(backendMessage);
            ListenResponse response = new ListenResponse
            {
                Message = clientMessage
            };

            // do not call clients.Count since it is expensive and uses locks
            int successCount = 0;
            int allCount = 0;

            foreach (IStreamer<ListenResponse> streamer in clients)
            {
                if (streamer.AddMessage(response))
                {
                    successCount++;
                }

                allCount++;
            }

            if (successCount < allCount)
            {
                _logger.LogWarning("Connected recipients ({0} out of {1}) were sent message {2}.", successCount, allCount, backendMessage);
            }
            else
            {
                _logger.LogTrace("Connected recipients (all {0}) were sent message {1}.", successCount, backendMessage);
            }
        }

        public ValueTask Handle(PartitionsChangedEventData eventData)
        {
            PartitionRange partitions = _messagingConfiguration.GetServerPartitions(_backendOptions.ServerID);
            // TODO: send a message to the clients connected to the server who should move to other server
            _consumer.Assign(_backendOptions.MessagesTopicName, partitions, _partitionFlyweight);
            return ValueTask.CompletedTask;
        }
    }
}

using System.Collections.Generic;
using System.Threading;
using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;
using CecoChat.DependencyInjection;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Clients;
using CecoChat.Server;
using CecoChat.Server.Backend;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public sealed class MessagesToReceiversConsumer : IBackendConsumer
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly ITopicPartitionFlyweight _partitionFlyweight;
        private readonly IKafkaConsumer<Null, BackendMessage> _consumer;
        private readonly IClientContainer _clientContainer;
        private readonly IClientBackendMapper _mapper;

        public MessagesToReceiversConsumer(
            ILogger<MessagesToReceiversConsumer> logger,
            IOptions<BackendOptions> backendOptions,
            ITopicPartitionFlyweight partitionFlyweight,
            IFactory<IKafkaConsumer<Null, BackendMessage>> consumerFactory,
            IClientContainer clientContainer,
            IClientBackendMapper mapper)
        {
            _logger = logger;
            _backendOptions = backendOptions.Value;
            _partitionFlyweight = partitionFlyweight;
            _consumer = consumerFactory.Create();
            _clientContainer = clientContainer;
            _mapper = mapper;
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }

        public void Prepare()
        {
            _consumer.Initialize(_backendOptions.Kafka, new BackendMessageDeserializer());
            _consumer.Assign(
                _backendOptions.MessagesTopicName,
                minPartition: 0,
                maxPartition: _backendOptions.MessagesTopicPartitionCount - 1,
                _partitionFlyweight);
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
    }
}

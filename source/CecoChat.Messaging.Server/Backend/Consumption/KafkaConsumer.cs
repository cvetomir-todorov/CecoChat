using System;
using System.Collections.Generic;
using System.Threading;
using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;
using CecoChat.Messaging.Server.Backend.Production;
using CecoChat.Messaging.Server.Clients;
using CecoChat.Server;
using CecoChat.Server.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend.Consumption
{
    public sealed class KafkaConsumer : IBackendConsumer
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _options;
        private readonly IClientContainer _clientContainer;
        private readonly IClientBackendMapper _mapper;
        private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
        private readonly IConsumer<Null, BackendMessage> _consumer;

        public KafkaConsumer(
            ILogger<KafkaProducer> logger,
            IOptions<BackendOptions> options,
            IClientContainer clientContainer,
            IClientBackendMapper mapper,
            ITopicPartitionFlyweight topicPartitionFlyweight)
        {
            _logger = logger;
            _options = options.Value;
            _clientContainer = clientContainer;
            _mapper = mapper;
            _topicPartitionFlyweight = topicPartitionFlyweight;

            ConsumerConfig configuration = new()
            {
                BootstrapServers = string.Join(separator: ',', _options.BootstrapServers),
                GroupId = _options.ConsumerGroupID,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnablePartitionEof = false,
                AllowAutoCreateTopics = false,
                EnableAutoCommit = false
            };

            _consumer = new ConsumerBuilder<Null, BackendMessage>(configuration)
                .SetValueDeserializer(new KafkaBackendMessageDeserializer())
                .Build();
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }

        public void Prepare()
        {
            List<TopicPartition> allPartitions = new(capacity: _options.MessagesTopicPartitionCount);
            for (int partition = 0; partition < _options.MessagesTopicPartitionCount; ++partition)
            {
                TopicPartition topicPartition = _topicPartitionFlyweight.GetMessagesTopicPartition(partition);
                allPartitions.Add(topicPartition);
            }

            _consumer.Assign(allPartitions);
        }

        public void Start(CancellationToken ct)
        {
            _logger.LogInformation("Start backend consumption.");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    ConsumeResult<Null, BackendMessage> consumeResult = _consumer.Consume(ct);
                    ProcessMessage(consumeResult.Message.Value);
                    _consumer.Commit(consumeResult);
                }
                catch (AccessViolationException accessViolationException)
                {
                    HandleConsumerDisposal(accessViolationException, ct);
                }
                catch (ObjectDisposedException objectDisposedException)
                {
                    HandleConsumerDisposal(objectDisposedException, ct);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error during backend consumption.");
                }
            }

            _logger.LogInformation("Stopped backend consumption.");
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

        private void HandleConsumerDisposal(Exception exception, CancellationToken ct)
        {
            if (!ct.IsCancellationRequested)
            {
                _logger.LogError(exception, "Kafka consumer is disposed without cancellation being requested.");
            }
        }
    }
}

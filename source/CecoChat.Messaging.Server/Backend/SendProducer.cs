using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CecoChat.Contracts;
using CecoChat.Contracts.Backend;
using CecoChat.Contracts.Client;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Clients;
using CecoChat.Server.Backend;
using Confluent.Kafka;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public interface ISendProducer : IDisposable
    {
        int PartitionCount { get; set; }

        void ProduceMessage(BackendMessage message);
    }

    public sealed class SendProducer : ISendProducer
    {
        private readonly ILogger _logger;
        private readonly IClock _clock;
        private readonly IBackendOptions _backendOptions;
        private readonly IPartitionUtility _partitionUtility;
        private readonly ITopicPartitionFlyweight _partitionFlyweight;
        private readonly IKafkaProducer<Null, BackendMessage> _producer;
        private readonly IClientContainer _clientContainer;

        public SendProducer(
            ILogger<SendProducer> logger,
            IClock clock,
            IOptions<BackendOptions> backendOptions,
            IHostApplicationLifetime applicationLifetime,
            IPartitionUtility partitionUtility,
            ITopicPartitionFlyweight partitionFlyweight,
            IFactory<IKafkaProducer<Null, BackendMessage>> producerFactory,
            IClientContainer clientContainer)
        {
            _logger = logger;
            _clock = clock;
            _backendOptions = backendOptions.Value;
            _partitionUtility = partitionUtility;
            _partitionFlyweight = partitionFlyweight;
            _producer = producerFactory.Create();
            _clientContainer = clientContainer;

            _producer.Initialize(_backendOptions.Kafka, _backendOptions.SendProducer, new BackendMessageSerializer());
            applicationLifetime.ApplicationStopping.Register(_producer.FlushPendingMessages);
        }

        public void Dispose()
        {
            _producer.Dispose();
        }

        public int PartitionCount { get; set; }

        public void ProduceMessage(BackendMessage message)
        {
            int partition = _partitionUtility.ChoosePartition(message.ReceiverId, PartitionCount);
            TopicPartition topicPartition = _partitionFlyweight.GetTopicPartition(_backendOptions.MessagesTopicName, partition);
            Message<Null, BackendMessage> kafkaMessage = new() {Value = message};

            _producer.Produce(kafkaMessage, topicPartition, DeliveryHandler);
        }

        private void DeliveryHandler(bool isDelivered, DeliveryReport<Null, BackendMessage> report, Activity activity)
        {
            BackendMessage backendMessage = report.Message.Value;
            IEnumerable<IStreamer<ListenResponse>> clients = _clientContainer.EnumerateClients(backendMessage.SenderId);
            Guid clientID = backendMessage.ClientId.ToGuid();
            IStreamer<ListenResponse> client = clients.FirstOrDefault(c => c.ClientID == clientID);

            if (client != null)
            {
                AckType ackType = isDelivered ? AckType.Processed : AckType.Lost;
                ClientMessage ackMessage = new()
                {
                    Type = ClientMessageType.Ack,
                    AckType = ackType,
                    Timestamp = _clock.GetNowUtc().ToTimestamp(),
                    MessageId = backendMessage.MessageId,
                    MessageIdSnowflake = backendMessage.MessageIdSnowflake,
                    SenderId = backendMessage.SenderId,
                    ReceiverId = backendMessage.ReceiverId
                };

                ListenResponse response = new() {Message = ackMessage};
                client.EnqueueMessage(response, parentActivity: activity);
                _logger.LogTrace("Sent ack to {0} for message {1}.", clientID, ackMessage);
            }
        }
    }
}

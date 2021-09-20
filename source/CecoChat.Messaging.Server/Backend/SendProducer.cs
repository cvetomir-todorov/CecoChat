using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CecoChat.Contracts;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Client;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Clients;
using CecoChat.Server.Backend;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backend
{
    public interface ISendProducer : IDisposable
    {
        int PartitionCount { get; set; }

        void ProduceMessage(BackplaneMessage message);
    }

    public sealed class SendProducer : ISendProducer
    {
        private readonly ILogger _logger;
        private readonly IBackendOptions _backendOptions;
        private readonly IPartitionUtility _partitionUtility;
        private readonly ITopicPartitionFlyweight _partitionFlyweight;
        private readonly IKafkaProducer<Null, BackplaneMessage> _producer;
        private readonly IClientContainer _clientContainer;

        public SendProducer(
            ILogger<SendProducer> logger,
            IOptions<BackendOptions> backendOptions,
            IHostApplicationLifetime applicationLifetime,
            IPartitionUtility partitionUtility,
            ITopicPartitionFlyweight partitionFlyweight,
            IFactory<IKafkaProducer<Null, BackplaneMessage>> producerFactory,
            IClientContainer clientContainer)
        {
            _logger = logger;
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

        public void ProduceMessage(BackplaneMessage message)
        {
            int partition = _partitionUtility.ChoosePartition(message.ReceiverId, PartitionCount);
            TopicPartition topicPartition = _partitionFlyweight.GetTopicPartition(_backendOptions.MessagesTopicName, partition);
            Message<Null, BackplaneMessage> kafkaMessage = new() {Value = message};

            _producer.Produce(kafkaMessage, topicPartition, DeliveryHandler);
        }

        private void DeliveryHandler(bool isDelivered, DeliveryReport<Null, BackplaneMessage> report, Activity activity)
        {
            BackplaneMessage backplaneMessage = report.Message.Value;
            IEnumerable<IStreamer<ListenResponse>> clients = _clientContainer.EnumerateClients(backplaneMessage.SenderId);
            Guid clientID = backplaneMessage.ClientId.ToGuid();
            IStreamer<ListenResponse> client = clients.FirstOrDefault(c => c.ClientID == clientID);

            if (client != null)
            {
                AckType ackType = isDelivered ? AckType.Processed : AckType.Lost;
                ClientMessage ackMessage = new()
                {
                    Type = ClientMessageType.Ack,
                    AckType = ackType,
                    MessageId = backplaneMessage.MessageId,
                    SenderId = backplaneMessage.SenderId,
                    ReceiverId = backplaneMessage.ReceiverId
                };

                ListenResponse response = new() {Message = ackMessage};
                client.EnqueueMessage(response, parentActivity: activity);
                _logger.LogTrace("Sent ack to {0} for message {1}.", clientID, ackMessage);
            }
        }
    }
}

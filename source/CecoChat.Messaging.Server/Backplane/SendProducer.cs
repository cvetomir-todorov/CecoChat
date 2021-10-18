﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using CecoChat.Contracts;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;
using CecoChat.Kafka;
using CecoChat.Messaging.Server.Clients;
using CecoChat.Server.Backplane;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Server.Backplane
{
    public interface ISendProducer : IDisposable
    {
        int PartitionCount { get; set; }

        void ProduceMessage(BackplaneMessage message);
    }

    public sealed class SendProducer : ISendProducer
    {
        private readonly ILogger _logger;
        private readonly BackplaneOptions _backplaneOptions;
        private readonly IPartitionUtility _partitionUtility;
        private readonly ITopicPartitionFlyweight _partitionFlyweight;
        private readonly IKafkaProducer<Null, BackplaneMessage> _producer;
        private readonly IClientContainer _clientContainer;

        public SendProducer(
            ILogger<SendProducer> logger,
            IOptions<BackplaneOptions> backplaneOptions,
            IHostApplicationLifetime applicationLifetime,
            IPartitionUtility partitionUtility,
            ITopicPartitionFlyweight partitionFlyweight,
            IFactory<IKafkaProducer<Null, BackplaneMessage>> producerFactory,
            IClientContainer clientContainer)
        {
            _logger = logger;
            _backplaneOptions = backplaneOptions.Value;
            _partitionUtility = partitionUtility;
            _partitionFlyweight = partitionFlyweight;
            _producer = producerFactory.Create();
            _clientContainer = clientContainer;

            _producer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.SendProducer, new BackplaneMessageSerializer());
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
            TopicPartition topicPartition = _partitionFlyweight.GetTopicPartition(_backplaneOptions.MessagesTopicName, partition);
            Message<Null, BackplaneMessage> kafkaMessage = new() {Value = message};

            _producer.Produce(kafkaMessage, topicPartition, DeliveryHandler);
        }

        private void DeliveryHandler(bool isDelivered, DeliveryReport<Null, BackplaneMessage> report, Activity activity)
        {
            BackplaneMessage backplaneMessage = report.Message.Value;

            DeliveryStatus status = isDelivered ? DeliveryStatus.Processed : DeliveryStatus.Lost;
            ListenResponse deliveryResponse = new()
            {
                MessageId = backplaneMessage.MessageId,
                SenderId = backplaneMessage.SenderId,
                ReceiverId = backplaneMessage.ReceiverId,
                Type = MessageType.Delivery,
                Status = status
            };
            Guid clientID = backplaneMessage.ClientId.ToGuid();
            IEnumerable<IStreamer<ListenResponse>> clients = _clientContainer.EnumerateClients(backplaneMessage.SenderId);

            foreach (IStreamer<ListenResponse> client in clients)
            {
                client.EnqueueMessage(deliveryResponse, parentActivity: activity);
                _logger.LogTrace("Sent delivery response to client {0} for message {1}.", clientID, deliveryResponse);
            }
        }
    }
}

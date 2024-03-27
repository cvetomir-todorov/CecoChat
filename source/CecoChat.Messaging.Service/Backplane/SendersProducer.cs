using System.Diagnostics;
using CecoChat.Contracts.Backplane;
using CecoChat.Messaging.Contracts;
using CecoChat.Messaging.Service.Clients;
using CecoChat.Messaging.Service.Telemetry;
using CecoChat.Server.Backplane;
using Common;
using Common.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Service.Backplane;

public interface ISendersProducer : IDisposable
{
    int PartitionCount { get; set; }

    void ProduceMessage(BackplaneMessage message);
}

public sealed class SendersProducer : ISendersProducer
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IPartitioner _partitioner;
    private readonly ITopicPartitionFlyweight _partitionFlyweight;
    private readonly IKafkaProducer<Null, BackplaneMessage> _producer;
    private readonly IClientContainer _clientContainer;
    private readonly IMessagingTelemetry _messagingTelemetry;

    public SendersProducer(
        ILogger<SendersProducer> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        IHostApplicationLifetime applicationLifetime,
        IPartitioner partitioner,
        ITopicPartitionFlyweight partitionFlyweight,
        IFactory<IKafkaProducer<Null, BackplaneMessage>> producerFactory,
        IClientContainer clientContainer,
        IMessagingTelemetry messagingTelemetry)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _partitioner = partitioner;
        _partitionFlyweight = partitionFlyweight;
        _producer = producerFactory.Create();
        _clientContainer = clientContainer;
        _messagingTelemetry = messagingTelemetry;

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
        int partition = _partitioner.ChoosePartition(message.TargetUserId, PartitionCount);
        TopicPartition topicPartition = _partitionFlyweight.GetTopicPartition(_backplaneOptions.TopicMessagesByReceiver, partition);
        Message<Null, BackplaneMessage> kafkaMessage = new() { Value = message };

        _producer.Produce(kafkaMessage, topicPartition, DeliveryHandler);
    }

    private void DeliveryHandler(bool isDelivered, DeliveryReport<Null, BackplaneMessage> report, Activity? produceActivity)
    {
        // add any child activities of the delivery handler to the active activity when the message was produced
        // and restore the previous activity afterwards
        Activity? previousActivity = Activity.Current;
        Activity.Current = produceActivity;

        try
        {
            NotifyDelivery(isDelivered, report);
        }
        finally
        {
            Activity.Current = previousActivity;
        }
    }

    private void NotifyDelivery(bool isDelivered, DeliveryReport<Null, BackplaneMessage> report)
    {
        BackplaneMessage backplaneMessage = report.Message.Value;
        if (isDelivered)
        {
            UpdateMetrics(backplaneMessage);
        }

        CecoChat.Messaging.Contracts.DeliveryStatus deliveryStatus = isDelivered ?
            CecoChat.Messaging.Contracts.DeliveryStatus.Processed :
            CecoChat.Messaging.Contracts.DeliveryStatus.Lost;
        ListenNotification notification = new()
        {
            MessageId = backplaneMessage.MessageId,
            SenderId = backplaneMessage.SenderId,
            ReceiverId = backplaneMessage.ReceiverId,
            Type = CecoChat.Messaging.Contracts.MessageType.DeliveryStatus,
            DeliveryStatus = deliveryStatus
        };

        long groupUserId = backplaneMessage.SenderId;
        _clientContainer
            .NotifyInGroup(notification, groupUserId)
            .ContinueWith(task =>
            {
                _logger.LogError(task.Exception, "Failed to send a delivery notification to clients in group {GroupId} about message {MessageId}",
                    groupUserId, notification.MessageId);
            }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private void UpdateMetrics(BackplaneMessage backplaneMessage)
    {
        switch (backplaneMessage.Type)
        {
            case CecoChat.Contracts.Backplane.MessageType.PlainText:
                _messagingTelemetry.NotifyPlainTextProcessed();
                break;
            case CecoChat.Contracts.Backplane.MessageType.File:
                _messagingTelemetry.NotifyFileProcessed();
                break;
            case CecoChat.Contracts.Backplane.MessageType.Reaction:
                if (!string.IsNullOrWhiteSpace(backplaneMessage.Reaction.Reaction))
                {
                    _messagingTelemetry.NotifyReactionProcessed();
                }
                else
                {
                    _messagingTelemetry.NotifyUnreactionProcessed();
                }
                break;
        }
    }
}

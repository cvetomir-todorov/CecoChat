using System.Diagnostics;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;
using CecoChat.Kafka;
using CecoChat.Server.Backplane;
using CecoChat.Server.Messaging.Clients;
using CecoChat.Server.Messaging.Telemetry;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Messaging.Backplane;

public interface ISendersProducer : IDisposable
{
    int PartitionCount { get; set; }

    void ProduceMessage(BackplaneMessage message);
}

public sealed class SendersProducer : ISendersProducer
{
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IPartitionUtility _partitionUtility;
    private readonly ITopicPartitionFlyweight _partitionFlyweight;
    private readonly IKafkaProducer<Null, BackplaneMessage> _producer;
    private readonly IClientContainer _clientContainer;
    private readonly IMessagingTelemetry _messagingTelemetry;

    public SendersProducer(
        IOptions<BackplaneOptions> backplaneOptions,
        IHostApplicationLifetime applicationLifetime,
        IPartitionUtility partitionUtility,
        ITopicPartitionFlyweight partitionFlyweight,
        IFactory<IKafkaProducer<Null, BackplaneMessage>> producerFactory,
        IClientContainer clientContainer,
        IMessagingTelemetry messagingTelemetry)
    {
        _backplaneOptions = backplaneOptions.Value;
        _partitionUtility = partitionUtility;
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
        int partition = _partitionUtility.ChoosePartition(message.ReceiverId, PartitionCount);
        TopicPartition topicPartition = _partitionFlyweight.GetTopicPartition(_backplaneOptions.TopicMessagesByReceiver, partition);
        Message<Null, BackplaneMessage> kafkaMessage = new() { Value = message };

        _producer.Produce(kafkaMessage, topicPartition, DeliveryHandler);
    }

    private void DeliveryHandler(bool isDelivered, DeliveryReport<Null, BackplaneMessage> report, Activity activity)
    {
        Activity? previousActivity = Activity.Current;
        Activity.Current = activity;

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

        Contracts.Messaging.DeliveryStatus deliveryStatus = isDelivered ? Contracts.Messaging.DeliveryStatus.Processed : Contracts.Messaging.DeliveryStatus.Lost;
        ListenNotification deliveryNotification = new()
        {
            MessageId = backplaneMessage.MessageId,
            SenderId = backplaneMessage.SenderId,
            ReceiverId = backplaneMessage.ReceiverId,
            Type = Contracts.Messaging.MessageType.DeliveryStatus,
            DeliveryStatus = deliveryStatus
        };

        _clientContainer.NotifyInGroup(deliveryNotification, backplaneMessage.SenderId);
    }

    private void UpdateMetrics(BackplaneMessage backplaneMessage)
    {
        switch (backplaneMessage.Type)
        {
            case Contracts.Backplane.MessageType.Data:
                if (backplaneMessage.Data.Type == Contracts.Backplane.DataType.PlainText)
                {
                    _messagingTelemetry.NotifyPlainTextProcessed();
                }
                break;
            case Contracts.Backplane.MessageType.Reaction:
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

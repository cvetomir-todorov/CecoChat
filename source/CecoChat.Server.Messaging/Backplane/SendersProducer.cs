using System.Diagnostics;
using CecoChat.Contracts;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.Messaging;
using CecoChat.Kafka;
using CecoChat.Server.Backplane;
using CecoChat.Server.Messaging.Clients.Streaming;
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
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IPartitionUtility _partitionUtility;
    private readonly ITopicPartitionFlyweight _partitionFlyweight;
    private readonly IKafkaProducer<Null, BackplaneMessage> _producer;
    private readonly IClientContainer _clientContainer;

    public SendersProducer(
        ILogger<SendersProducer> logger,
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
        TopicPartition topicPartition = _partitionFlyweight.GetTopicPartition(_backplaneOptions.TopicMessagesByReceiver, partition);
        Message<Null, BackplaneMessage> kafkaMessage = new() { Value = message };

        _producer.Produce(kafkaMessage, topicPartition, DeliveryHandler);
    }

    private void DeliveryHandler(bool isDelivered, DeliveryReport<Null, BackplaneMessage> report, Activity activity)
    {
        BackplaneMessage backplaneMessage = report.Message.Value;

        Contracts.Messaging.DeliveryStatus status = isDelivered ? Contracts.Messaging.DeliveryStatus.Processed : Contracts.Messaging.DeliveryStatus.Lost;
        ListenNotification deliveryNotification = new()
        {
            MessageId = backplaneMessage.MessageId,
            SenderId = backplaneMessage.SenderId,
            ReceiverId = backplaneMessage.ReceiverId,
            Type = Contracts.Messaging.MessageType.Delivery,
            Status = status
        };
        Guid clientID = backplaneMessage.ClientId.ToGuid();
        IEnumerable<IStreamer<ListenNotification>> clients = _clientContainer.EnumerateClients(backplaneMessage.SenderId);

        foreach (IStreamer<ListenNotification> client in clients)
        {
            client.EnqueueMessage(deliveryNotification, parentActivity: activity);
            _logger.LogTrace("Sent delivery notification to client {0} for message {1}.", clientID, deliveryNotification);
        }
    }
}
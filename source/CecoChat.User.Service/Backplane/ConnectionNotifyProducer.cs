using CecoChat.Contracts.Backplane;
using CecoChat.Server.Backplane;
using CecoChat.User.Contracts;
using Common;
using Common.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.User.Service.Backplane;

public interface IConnectionNotifyProducer
{
    int PartitionCount { get; set; }

    void NotifyConnectionChange(long userId, Connection connection);
}

public sealed class ConnectionNotifyProducer : IConnectionNotifyProducer
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IPartitioner _partitioner;
    private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
    private readonly IKafkaProducer<Null, BackplaneMessage> _producer;

    public ConnectionNotifyProducer(
        ILogger<ConnectionNotifyProducer> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        IHostApplicationLifetime applicationLifetime,
        IPartitioner partitioner,
        ITopicPartitionFlyweight topicPartitionFlyweight,
        IKafkaProducer<Null, BackplaneMessage> producer)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _partitioner = partitioner;
        _topicPartitionFlyweight = topicPartitionFlyweight;
        _producer = producer;

        _producer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.ConnectionsProducer, new BackplaneMessageSerializer());
        applicationLifetime.ApplicationStopping.Register(_producer.FlushPendingMessages);
    }

    public int PartitionCount { get; set; }

    public void NotifyConnectionChange(long userId, Connection connection)
    {
        BackplaneMessage messageToUser = MapConnection(userId, connection);
        BackplaneMessage messageToConnection = MapConnection(userId, connection);

        messageToUser.TargetUserId = userId;
        messageToConnection.TargetUserId = connection.ConnectionId;

        SendMessage(messageToUser);
        SendMessage(messageToConnection);

        _logger.LogTrace("User {UserId} sent notifications about updating its connection to {ConnectionId} by changing status to {ConnectionStatus}",
            userId, connection.ConnectionId, connection.Status);
    }

    private static BackplaneMessage MapConnection(long userId, Connection connection)
    {
        CecoChat.Contracts.Backplane.ConnectionStatus status;

        switch (connection.Status)
        {
            case CecoChat.User.Contracts.ConnectionStatus.NotConnected:
                status = CecoChat.Contracts.Backplane.ConnectionStatus.NotConnected;
                break;
            case CecoChat.User.Contracts.ConnectionStatus.Pending:
                status = CecoChat.Contracts.Backplane.ConnectionStatus.Pending;
                break;
            case CecoChat.User.Contracts.ConnectionStatus.Connected:
                status = CecoChat.Contracts.Backplane.ConnectionStatus.Connected;
                break;
            default:
                throw new EnumValueNotSupportedException(connection.Status);
        }

        // omit target user ID which will be set by the caller
        return new BackplaneMessage
        {
            SenderId = userId,
            ReceiverId = connection.ConnectionId,
            Type = MessageType.Connection,
            Status = DeliveryStatus.Processed,
            Connection = new BackplaneConnection
            {
                Status = status,
                Version = connection.Version
            }
        };
    }

    private void SendMessage(BackplaneMessage message)
    {
        int partition = _partitioner.ChoosePartition(message.TargetUserId, PartitionCount);
        TopicPartition topicPartition = _topicPartitionFlyweight.GetTopicPartition(_backplaneOptions.TopicMessagesByReceiver, partition);
        Message<Null, BackplaneMessage> kafkaMessage = new() { Value = message };

        _producer.Produce(kafkaMessage, topicPartition, deliveryHandler: null);
    }
}

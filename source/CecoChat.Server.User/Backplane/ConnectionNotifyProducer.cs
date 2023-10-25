using CecoChat.Client.IdGen;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.User;
using CecoChat.Kafka;
using CecoChat.Server.Backplane;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.User.Backplane;

public interface IConnectionNotifyProducer
{
    int PartitionCount { get; set; }

    Task NotifyConnectionChange(long userId, Connection connection, CancellationToken ct);
}

public sealed class ConnectionNotifyProducer : IConnectionNotifyProducer
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IPartitionUtility _partitionUtility;
    private readonly ITopicPartitionFlyweight _topicPartitionFlyweight;
    private readonly IIdGenClient _idGenClient;
    private readonly IKafkaProducer<Null, BackplaneMessage> _producer;

    public ConnectionNotifyProducer(
        ILogger<ConnectionNotifyProducer> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        IHostApplicationLifetime applicationLifetime,
        IPartitionUtility partitionUtility,
        ITopicPartitionFlyweight topicPartitionFlyweight,
        IIdGenClient idGenClient,
        IKafkaProducer<Null, BackplaneMessage> producer)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _partitionUtility = partitionUtility;
        _topicPartitionFlyweight = topicPartitionFlyweight;
        _idGenClient = idGenClient;
        _producer = producer;

        _producer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.ConnectionsProducer, new BackplaneMessageSerializer());
        applicationLifetime.ApplicationStopping.Register(_producer.FlushPendingMessages);
    }

    public int PartitionCount { get; set; }

    public async Task NotifyConnectionChange(long userId, Connection connection, CancellationToken ct)
    {
        BackplaneMessage messageToUser = MapConnection(userId, connection);
        BackplaneMessage messageToConnection = MapConnection(userId, connection);

        messageToUser.TargetUserId = userId;
        messageToConnection.TargetUserId = connection.ConnectionId;

        Task sendToUser = SendMessage(messageToUser, ct);
        Task sendToConnection = SendMessage(messageToConnection, ct);

        await Task.WhenAll(sendToUser, sendToConnection);
        _logger.LogTrace("User {UserId} sent notifications about updating its connection to {ConnectionId} by changing status to {ConnectionStatus}",
            userId, connection.ConnectionId, connection.Status);
    }

    private BackplaneMessage MapConnection(long userId, Connection connection)
    {
        Contracts.Backplane.ConnectionStatus status;

        switch (connection.Status)
        {
            case Contracts.User.ConnectionStatus.NotConnected:
                status = Contracts.Backplane.ConnectionStatus.NotConnected;
                break;
            case Contracts.User.ConnectionStatus.Pending:
                status = Contracts.Backplane.ConnectionStatus.Pending;
                break;
            case Contracts.User.ConnectionStatus.Connected:
                status = Contracts.Backplane.ConnectionStatus.Connected;
                break;
            default:
                throw new EnumValueNotSupportedException(connection.Status);
        }

        // omit message ID and target user ID which will be set by the caller
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

    private async Task SendMessage(BackplaneMessage message, CancellationToken ct)
    {
        GetIdResult idResult = await _idGenClient.GetId(ct);
        if (!idResult.Success)
        {
            throw new InvalidOperationException("Failed to obtain a message ID.");
        }

        message.MessageId = idResult.Id;

        int partition = _partitionUtility.ChoosePartition(message.TargetUserId, PartitionCount);
        TopicPartition topicPartition = _topicPartitionFlyweight.GetTopicPartition(_backplaneOptions.TopicMessagesByReceiver, partition);
        Message<Null, BackplaneMessage> kafkaMessage = new() { Value = message };

        _producer.Produce(kafkaMessage, topicPartition, deliveryHandler: null);
    }
}

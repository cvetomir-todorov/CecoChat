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

/// <summary>
/// Consumes messages partitioned by receiver ID and sends them to the connected receiver clients.
/// </summary>
public interface IReceiversConsumer : IDisposable
{
    void Prepare(PartitionRange partitions);

    void Start(CancellationToken ct);

    string ConsumerId { get; }
}

public sealed class ReceiversConsumer : IReceiversConsumer
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly ITopicPartitionFlyweight _partitionFlyweight;
    private readonly IKafkaConsumer<Null, BackplaneMessage> _consumer;
    private readonly IClientContainer _clientContainer;
    private readonly IContractMapper _mapper;
    private bool _isInitialized;
    private readonly object _initializationLock;

    public ReceiversConsumer(
        ILogger<ReceiversConsumer> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        ITopicPartitionFlyweight partitionFlyweight,
        IFactory<IKafkaConsumer<Null, BackplaneMessage>> consumerFactory,
        IClientContainer clientContainer,
        IContractMapper mapper)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _partitionFlyweight = partitionFlyweight;
        _consumer = consumerFactory.Create();
        _clientContainer = clientContainer;
        _mapper = mapper;

        _initializationLock = new object();
    }

    public void Dispose()
    {
        _consumer.Dispose();
    }

    public void Prepare(PartitionRange partitions)
    {
        lock (_initializationLock)
        {
            if (!_isInitialized)
            {
                _consumer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.ReceiversConsumer, new BackplaneMessageDeserializer());
                _isInitialized = true;
            }
        }

        _consumer.Assign(_backplaneOptions.TopicMessagesByReceiver, partitions, _partitionFlyweight);
    }

    public void Start(CancellationToken ct)
    {
        _logger.LogInformation("Start sending messages to receivers");

        while (!ct.IsCancellationRequested)
        {
            _consumer.Consume(consumeResult =>
            {
                ProcessMessage(consumeResult.Message.Value);
            }, ct);
        }

        _logger.LogInformation("Stopped sending messages to receivers");
    }

    public string ConsumerId => _backplaneOptions.ReceiversConsumer.ConsumerGroupId;

    private void ProcessMessage(BackplaneMessage backplaneMessage)
    {
        EnqueueMessageForReceiverClients(backplaneMessage);
    }

    private void EnqueueMessageForReceiverClients(BackplaneMessage backplaneMessage)
    {
        // do not call clients.Count since it is expensive and uses locks
        int successCount = 0;
        int allCount = 0;

        Guid senderClientId = backplaneMessage.ClientId.ToGuid();
        IEnumerable<IStreamer<ListenNotification>> receiverClients = _clientContainer.EnumerateClients(backplaneMessage.ReceiverId);
        ListenNotification? notification = null;

        foreach (IStreamer<ListenNotification> receiverClient in receiverClients)
        {
            if (receiverClient.ClientId != senderClientId)
            {
                notification ??= _mapper.CreateListenNotification(backplaneMessage);

                if (receiverClient.EnqueueMessage(notification, parentActivity: Activity.Current))
                {
                    successCount++;
                }

                allCount++;
            }
        }

        LogLevel logLevel = successCount < allCount ? LogLevel.Warning : LogLevel.Trace;
        _logger.Log(logLevel, "Connected recipients with ID {ReceiverId} ({SuccessCount} out of {AllCount}) were queued message {MessageId} of type {MessageType} from user {SenderId}",
            backplaneMessage.ReceiverId, successCount, allCount, backplaneMessage.MessageId, backplaneMessage.Type, backplaneMessage.SenderId);
    }
}

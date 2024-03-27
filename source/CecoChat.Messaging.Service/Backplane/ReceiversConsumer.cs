using CecoChat.Contracts.Backplane;
using CecoChat.Messaging.Contracts;
using CecoChat.Messaging.Service.Clients;
using CecoChat.Server.Backplane;
using Common;
using Common.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Service.Backplane;

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
        ListenNotification notification = _mapper.CreateListenNotification(backplaneMessage);
        long groupUserId = backplaneMessage.TargetUserId;
        _clientContainer
            .NotifyInGroup(notification, groupUserId)
            .ContinueWith(task =>
            {
                _logger.LogError(task.Exception, "Failed to send message {MessageId} of type {MessageType} to clients in group {ClientGroup}",
                    notification.MessageId, notification.Type, groupUserId);
            }, TaskContinuationOptions.OnlyOnFaulted);
    }
}

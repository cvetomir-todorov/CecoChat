using CecoChat.Backplane;
using CecoChat.Backplane.Contracts;
using CecoChat.Chats.Contracts;
using CecoChat.Chats.Data.Entities.ChatMessages;
using Common;
using Common.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.Chats.Service.Backplane;

public interface IHistoryConsumer : IDisposable
{
    void Prepare();

    void Start(CancellationToken ct);

    string ConsumerId { get; }
}

public sealed class HistoryConsumer : IHistoryConsumer
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IKafkaConsumer<Null, BackplaneMessage> _consumer;
    private readonly IContractMapper _mapper;
    private readonly IChatMessageRepo _messageRepo;

    public HistoryConsumer(
        ILogger<HistoryConsumer> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        IFactory<IKafkaConsumer<Null, BackplaneMessage>> consumerFactory,
        IContractMapper mapper,
        IChatMessageRepo messageRepo)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _consumer = consumerFactory.Create();
        _mapper = mapper;
        _messageRepo = messageRepo;
    }

    public void Dispose()
    {
        _consumer.Dispose();
        _messageRepo.Dispose();
    }

    public void Prepare()
    {
        _consumer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.HistoryConsumer, new BackplaneMessageDeserializer());
        _consumer.Subscribe(_backplaneOptions.TopicMessagesByReceiver);
    }

    public void Start(CancellationToken ct)
    {
        _logger.LogInformation("Start creating history from messages");

        while (!ct.IsCancellationRequested)
        {
            _consumer.Consume(consumeResult =>
            {
                Process(consumeResult.Message.Value);
            }, ct);
        }

        _logger.LogInformation("Stopped creating history from messages");
    }

    public string ConsumerId => _backplaneOptions.HistoryConsumer.ConsumerGroupId;

    private void Process(BackplaneMessage backplaneMessage)
    {
        switch (backplaneMessage.Type)
        {
            case MessageType.PlainText:
                AddPlainText(backplaneMessage);
                break;
            case MessageType.File:
                AddFile(backplaneMessage);
                break;
            case MessageType.Reaction:
                AddReaction(backplaneMessage);
                break;
            case MessageType.Disconnect:
            case MessageType.Delivery:
            case MessageType.Connection:
                // ignore these
                break;
            default:
                throw new EnumValueNotSupportedException(backplaneMessage.Type);
        }
    }

    private void AddPlainText(BackplaneMessage backplaneMessage)
    {
        PlainTextMessage plainTextMessage = _mapper.CreatePlainTextMessage(backplaneMessage);
        _messageRepo.AddPlainTextMessage(plainTextMessage);
        _logger.LogTrace("Added plain text message {MessageId} between sender {SenderId} and receiver {ReceiverId}",
            backplaneMessage.MessageId, backplaneMessage.SenderId, backplaneMessage.ReceiverId);
    }

    private void AddFile(BackplaneMessage backplaneMessage)
    {
        FileMessage fileMessage = _mapper.CreateFileMessage(backplaneMessage);
        _messageRepo.AddFileMessage(fileMessage);
        _logger.LogTrace("Added file message {MessageId} between sender {SenderId} and receiver {ReceiverId}",
            backplaneMessage.MessageId, backplaneMessage.SenderId, backplaneMessage.ReceiverId);
    }

    private void AddReaction(BackplaneMessage backplaneMessage)
    {
        ReactionMessage reactionMessage = _mapper.CreateReactionMessage(backplaneMessage);
        if (reactionMessage.Type == NewReactionType.Set)
        {
            _messageRepo.SetReaction(reactionMessage);
            _logger.LogTrace("Added user {ReactorId} reaction {Reaction} to message {MessageId}",
                reactionMessage.ReactorId, reactionMessage.Reaction, reactionMessage.MessageId);
        }
        else
        {
            _messageRepo.UnsetReaction(reactionMessage);
            _logger.LogTrace("Removed user {ReactorId} reaction to message {MessageId}",
                reactionMessage.ReactorId, reactionMessage.MessageId);
        }
    }
}

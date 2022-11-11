using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.State;
using CecoChat.Data;
using CecoChat.Data.State.Repos;
using CecoChat.Kafka;
using CecoChat.Server.Backplane;
using CecoChat.Server.State.Clients;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.State.Backplane;

public interface IStateConsumer : IDisposable
{
    void Prepare();

    void StartConsumingReceiverMessages(CancellationToken ct);

    void StartConsumingSenderMessages(CancellationToken ct);

    string ReceiverConsumerId { get; }

    string SenderConsumerId { get; }
}

public sealed class StateConsumer : IStateConsumer
{
    private readonly ILogger _logger;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IKafkaConsumer<Null, BackplaneMessage> _receiversConsumer;
    private readonly IKafkaConsumer<Null, BackplaneMessage> _sendersConsumer;
    private readonly IChatStateRepo _repo;
    private readonly IStateCache _cache;

    public StateConsumer(
        ILogger<StateConsumer> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        IFactory<IKafkaConsumer<Null, BackplaneMessage>> consumerFactory,
        IChatStateRepo repo,
        IStateCache cache)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _receiversConsumer = consumerFactory.Create();
        _sendersConsumer = consumerFactory.Create();
        _repo = repo;
        _cache = cache;
    }

    public void Dispose()
    {
        _receiversConsumer.Dispose();
        _sendersConsumer.Dispose();
        _repo.Dispose();
    }

    public void Prepare()
    {
        _receiversConsumer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.ReceiversConsumer, new BackplaneMessageDeserializer());
        _sendersConsumer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.SendersConsumer, new BackplaneMessageDeserializer());

        _receiversConsumer.Subscribe(_backplaneOptions.TopicMessagesByReceiver);
        _sendersConsumer.Subscribe(_backplaneOptions.TopicMessagesBySender);
    }

    public void StartConsumingReceiverMessages(CancellationToken ct)
    {
        _logger.LogInformation("Start creating receiver state from messages");

        while (!ct.IsCancellationRequested)
        {
            _receiversConsumer.Consume(consumeResult =>
            {
                ProcessMessageForReceivers(consumeResult.Message.Value);
            }, ct);
        }

        _logger.LogInformation("Stopped creating receiver state from messages");
    }

    public void StartConsumingSenderMessages(CancellationToken ct)
    {
        _logger.LogInformation("Start creating sender state from messages");

        while (!ct.IsCancellationRequested)
        {
            _sendersConsumer.Consume(consumeResult =>
            {
                ProcessMessageForSenders(consumeResult.Message.Value);
            }, ct);
        }

        _logger.LogInformation("Stopped creating sender state from messages");
    }

    public string ReceiverConsumerId => _backplaneOptions.ReceiversConsumer.ConsumerGroupID;

    public string SenderConsumerId => _backplaneOptions.SendersConsumer.ConsumerGroupID;

    private void ProcessMessageForReceivers(BackplaneMessage backplaneMessage)
    {
        switch (backplaneMessage.Type)
        {
            case MessageType.Data:
                UpdateReceiverState(backplaneMessage);
                break;
            case MessageType.Delivery:
                // to be implemented
                break;
            case MessageType.Reaction:
                // ignore because the message ID is for a previously sent message
                break;
            case MessageType.Disconnect:
                // ignore these
                break;
            default:
                throw new EnumValueNotSupportedException(backplaneMessage.Type);
        }
    }

    private void ProcessMessageForSenders(BackplaneMessage backplaneMessage)
    {
        switch (backplaneMessage.Type)
        {
            case MessageType.Data:
                UpdateSenderState(backplaneMessage);
                break;
            case MessageType.Delivery:
                // to be implemented
                break;
            case MessageType.Reaction:
                // ignore because the message ID is for a previously sent message
                break;
            case MessageType.Disconnect:
                // ignore because this is designated to the client apps only
                break;
            default:
                throw new EnumValueNotSupportedException(backplaneMessage.Type);
        }
    }

    private void UpdateReceiverState(BackplaneMessage backplaneMessage)
    {
        long targetUserId = backplaneMessage.ReceiverId;
        string chatId = DataUtility.CreateChatID(backplaneMessage.SenderId, backplaneMessage.ReceiverId);
        ChatState receiverChat = GetChatFromDdIntoCache(targetUserId, chatId);

        bool needsUpdate = false;
        if (backplaneMessage.MessageId > receiverChat.NewestMessage)
        {
            receiverChat.NewestMessage = backplaneMessage.MessageId;
            needsUpdate = true;
        }
        if (backplaneMessage.MessageId > receiverChat.OtherUserDelivered)
        {
            receiverChat.OtherUserDelivered = backplaneMessage.MessageId;
            needsUpdate = true;
        }
        if (backplaneMessage.MessageId > receiverChat.OtherUserSeen)
        {
            receiverChat.OtherUserSeen = backplaneMessage.MessageId;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            _repo.UpdateChat(targetUserId, receiverChat);
            _logger.LogTrace("Updated receiver state for user {TargetUserId} chat {ChatId} with message {MessageId}", targetUserId, chatId, backplaneMessage.MessageId);
        }
    }

    private void UpdateSenderState(BackplaneMessage backplaneMessage)
    {
        long targetUserId = backplaneMessage.SenderId;
        string chatId = DataUtility.CreateChatID(backplaneMessage.SenderId, backplaneMessage.ReceiverId);
        ChatState senderChat = GetChatFromDdIntoCache(targetUserId, chatId);

        bool needsUpdate = false;
        if (backplaneMessage.MessageId > senderChat.NewestMessage)
        {
            senderChat.NewestMessage = backplaneMessage.MessageId;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            _repo.UpdateChat(targetUserId, senderChat);
            _logger.LogTrace("Updated sender state for user {TargetUserId} chat {ChatId} with message {MessageId}", targetUserId, chatId, backplaneMessage.MessageId);
        }
    }

    private ChatState GetChatFromDdIntoCache(long userId, string chatId)
    {
        if (!_cache.TryGetUserChat(userId, chatId, out ChatState? chat))
        {
            chat = _repo.GetChat(userId, chatId);
            chat ??= new ChatState { ChatId = chatId };
            _cache.AddUserChat(userId, chat);
        }

        return chat;
    }
}

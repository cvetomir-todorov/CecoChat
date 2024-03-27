using CecoChat.Chats.Contracts;
using CecoChat.Contracts.Backplane;
using CecoChat.Data;
using CecoChat.Data.Chats.Entities.UserChats;
using CecoChat.Server.Backplane;
using Common;
using Common.Kafka;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.Chats.Backplane;

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
    private readonly IUserChatsRepo _chatsRepo;

    public StateConsumer(
        ILogger<StateConsumer> logger,
        IOptions<BackplaneOptions> backplaneOptions,
        IFactory<IKafkaConsumer<Null, BackplaneMessage>> consumerFactory,
        IUserChatsRepo chatsRepo)
    {
        _logger = logger;
        _backplaneOptions = backplaneOptions.Value;
        _receiversConsumer = consumerFactory.Create();
        _sendersConsumer = consumerFactory.Create();
        _chatsRepo = chatsRepo;
    }

    public void Dispose()
    {
        _receiversConsumer.Dispose();
        _sendersConsumer.Dispose();
        _chatsRepo.Dispose();
    }

    public void Prepare()
    {
        _receiversConsumer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.ReceiversConsumer, new BackplaneMessageDeserializer());
        _sendersConsumer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.SendersConsumer, new BackplaneMessageDeserializer());

        _receiversConsumer.Subscribe(_backplaneOptions.TopicMessagesByReceiver);
        _sendersConsumer.Subscribe(_backplaneOptions.TopicMessagesByReceiver);
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

    public string ReceiverConsumerId => _backplaneOptions.ReceiversConsumer.ConsumerGroupId;

    public string SenderConsumerId => _backplaneOptions.SendersConsumer.ConsumerGroupId;

    private void ProcessMessageForReceivers(BackplaneMessage backplaneMessage)
    {
        switch (backplaneMessage.Type)
        {
            case MessageType.PlainText:
            case MessageType.File:
                UpdateReceiverState(backplaneMessage);
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
            case MessageType.Connection:
                // ignore because this is designated to the client apps only
                break;
            default:
                throw new EnumValueNotSupportedException(backplaneMessage.Type);
        }
    }

    private void ProcessMessageForSenders(BackplaneMessage backplaneMessage)
    {
        switch (backplaneMessage.Type)
        {
            case MessageType.PlainText:
            case MessageType.File:
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
            case MessageType.Connection:
                // ignore because this is designated to the client apps only
                break;
            default:
                throw new EnumValueNotSupportedException(backplaneMessage.Type);
        }
    }

    private void UpdateReceiverState(BackplaneMessage backplaneMessage)
    {
        long targetUserId = backplaneMessage.ReceiverId;
        long otherUserId = backplaneMessage.SenderId;
        string chatId = DataUtility.CreateChatId(backplaneMessage.SenderId, backplaneMessage.ReceiverId);
        ChatState receiverChat = GetUserChat(targetUserId, otherUserId, chatId);

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
            _chatsRepo.UpdateUserChat(targetUserId, receiverChat);
            _logger.LogTrace("Updated receiver state for user {TargetUserId} chat {ChatId} with message {MessageId}", targetUserId, chatId, backplaneMessage.MessageId);
        }
    }

    private void UpdateSenderState(BackplaneMessage backplaneMessage)
    {
        long targetUserId = backplaneMessage.SenderId;
        long otherUserId = backplaneMessage.ReceiverId;
        string chatId = DataUtility.CreateChatId(backplaneMessage.SenderId, backplaneMessage.ReceiverId);
        ChatState senderChat = GetUserChat(targetUserId, otherUserId, chatId);

        bool needsUpdate = false;
        if (backplaneMessage.MessageId > senderChat.NewestMessage)
        {
            senderChat.NewestMessage = backplaneMessage.MessageId;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            _chatsRepo.UpdateUserChat(targetUserId, senderChat);
            _logger.LogTrace("Updated sender state for user {TargetUserId} chat {ChatId} with message {MessageId}", targetUserId, chatId, backplaneMessage.MessageId);
        }
    }

    private ChatState GetUserChat(long userId, long otherUserId, string chatId)
    {
        ChatState? chat = _chatsRepo.GetUserChat(userId, chatId);
        chat ??= new ChatState
        {
            ChatId = chatId,
            OtherUserId = otherUserId
        };

        return chat;
    }
}

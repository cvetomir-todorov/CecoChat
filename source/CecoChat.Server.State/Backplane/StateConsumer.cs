using System;
using System.Threading;
using CecoChat.Contracts.Backplane;
using CecoChat.Contracts.State;
using CecoChat.Data;
using CecoChat.Data.State.Repos;
using CecoChat.Kafka;
using CecoChat.Server.Backplane;
using CecoChat.Server.State.Clients;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.State.Backplane
{
    public interface IStateConsumer : IDisposable
    {
        void Prepare();

        void StartConsumingReceiverMessages(CancellationToken ct);

        void StartConsumingSenderMessages(CancellationToken ct);

        string ReceiverConsumerID { get; }

        string SenderConsumerID { get; }
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
            _logger.LogInformation("Start creating receiver state from messages.");

            while (!ct.IsCancellationRequested)
            {
                _receiversConsumer.Consume(consumeResult =>
                {
                    ProcessMessageForReceivers(consumeResult.Message.Value);
                }, ct);
            }

            _logger.LogInformation("Stopped creating receiver state from messages.");
        }

        public void StartConsumingSenderMessages(CancellationToken ct)
        {
            _logger.LogInformation("Start creating sender state from messages.");

            while (!ct.IsCancellationRequested)
            {
                _sendersConsumer.Consume(consumeResult =>
                {
                    ProcessMessageForSenders(consumeResult.Message.Value);
                }, ct);
            }

            _logger.LogInformation("Stopped creating sender state from messages.");
        }

        public string ReceiverConsumerID => _backplaneOptions.ReceiversConsumer.ConsumerGroupID;

        public string SenderConsumerID => _backplaneOptions.SendersConsumer.ConsumerGroupID;

        private void ProcessMessageForReceivers(BackplaneMessage backplaneMessage)
        {
            switch (backplaneMessage.Type)
            {
                case MessageType.Data:
                case MessageType.Reaction:
                    UpdateReceiverState(backplaneMessage);
                    break;
                case MessageType.Delivery:
                    // to be implemented
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
                case MessageType.Reaction:
                    UpdateSenderState(backplaneMessage);
                    break;
                case MessageType.Delivery:
                    // to be implemented
                    break;
                case MessageType.Disconnect:
                    // ignore these
                    break;
                default:
                    throw new EnumValueNotSupportedException(backplaneMessage.Type);
            }
        }

        private void UpdateReceiverState(BackplaneMessage backplaneMessage)
        {
            long targetUserID = backplaneMessage.ReceiverId;
            string chatID = DataUtility.CreateChatID(backplaneMessage.SenderId, backplaneMessage.ReceiverId);
            ChatState receiverChat = GetChatFromDBIntoCache(targetUserID, chatID);
            receiverChat.NewestMessage = Math.Max(receiverChat.NewestMessage, backplaneMessage.MessageId);
            receiverChat.OtherUserDelivered = Math.Max(receiverChat.OtherUserDelivered, backplaneMessage.MessageId);
            receiverChat.OtherUserSeen = Math.Max(receiverChat.OtherUserSeen, backplaneMessage.MessageId);

            _repo.UpdateChat(targetUserID, receiverChat);
            _logger.LogTrace("Updated receiver state for user {0} chat {1} with message {2}.", targetUserID, chatID, backplaneMessage.MessageId);
        }

        private void UpdateSenderState(BackplaneMessage backplaneMessage)
        {
            long targetUserID = backplaneMessage.SenderId;
            string chatID = DataUtility.CreateChatID(backplaneMessage.SenderId, backplaneMessage.ReceiverId);
            ChatState senderChat = GetChatFromDBIntoCache(targetUserID, chatID);
            senderChat.NewestMessage = Math.Max(senderChat.NewestMessage, backplaneMessage.MessageId);

            _repo.UpdateChat(targetUserID, senderChat);
            _logger.LogTrace("Updated sender state for user {0} chat {1} with message {2}.", targetUserID, chatID, backplaneMessage.MessageId);
        }

        private ChatState GetChatFromDBIntoCache(long userID, string chatID)
        {
            if (!_cache.TryGetUserChat(userID, chatID, out ChatState chat))
            {
                chat = _repo.GetChat(userID, chatID);
                chat ??= new ChatState {ChatId = chatID};
                _cache.UpdateUserChat(userID, chat);
            }

            return chat;
        }
    }
}
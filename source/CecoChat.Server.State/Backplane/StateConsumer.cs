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

        void Start(CancellationToken ct);

        string ConsumerID { get; }
    }

    public sealed class StateConsumer : IStateConsumer
    {
        private readonly ILogger _logger;
        private readonly BackplaneOptions _backplaneOptions;
        private readonly IKafkaConsumer<Null, BackplaneMessage> _consumer;
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
            _consumer = consumerFactory.Create();
            _repo = repo;
            _cache = cache;
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }

        public void Prepare()
        {
            _consumer.Initialize(_backplaneOptions.Kafka, _backplaneOptions.StateConsumer, new BackplaneMessageDeserializer());
            _consumer.Subscribe(_backplaneOptions.TopicMessagesByReceiver);
        }

        public void Start(CancellationToken ct)
        {
            _logger.LogInformation("Start creating state from messages.");

            while (!ct.IsCancellationRequested)
            {
                _consumer.Consume(consumeResult =>
                {
                    Process(consumeResult.Message.Value);
                }, ct);
            }

            _logger.LogInformation("Stopped creating state from messages.");
        }

        public string ConsumerID => _backplaneOptions.StateConsumer.ConsumerGroupID;

        private void Process(BackplaneMessage backplaneMessage)
        {
            switch (backplaneMessage.Type)
            {
                case MessageType.Data:
                case MessageType.Reaction:
                    UpdateReceiverStateOnDataOrReaction(backplaneMessage);
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

        private void UpdateReceiverStateOnDataOrReaction(BackplaneMessage backplaneMessage)
        {
            string chatID = DataUtility.CreateChatID(backplaneMessage.SenderId, backplaneMessage.ReceiverId);
            ChatState receiverChat = GetChatFromDBIntoCache(backplaneMessage.ReceiverId, chatID);
            receiverChat.NewestMessage = Math.Max(receiverChat.NewestMessage, backplaneMessage.MessageId);
            receiverChat.OtherUserDelivered = Math.Max(receiverChat.OtherUserDelivered, backplaneMessage.MessageId);
            receiverChat.OtherUserSeen = Math.Max(receiverChat.OtherUserSeen, backplaneMessage.MessageId);

            _repo.UpdateChat(backplaneMessage.ReceiverId, receiverChat);
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
using System;
using System.Diagnostics;
using Cassandra;
using CecoChat.Contracts.History;
using CecoChat.Data.History.Instrumentation;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.History.Repos
{
    public interface INewMessageRepo
    {
        void Prepare();

        void AddMessage(DataMessage message);
    }

    internal sealed class NewMessageRepo : INewMessageRepo
    {
        private readonly ILogger _logger;
        private readonly IHistoryActivityUtility _historyActivityUtility;
        private readonly IDataUtility _dataUtility;
        private readonly IDataMapper _mapper;
        private readonly Lazy<PreparedStatement> _messagesForChatQuery;

        public NewMessageRepo(
            ILogger<NewMessageRepo> logger,
            IHistoryActivityUtility historyActivityUtility,
            IDataUtility dataUtility,
            IDataMapper mapper)
        {
            _logger = logger;
            _historyActivityUtility = historyActivityUtility;
            _dataUtility = dataUtility;
            _mapper = mapper;

            _messagesForChatQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(InsertIntoMessagesForChat));
        }

        private const string InsertIntoMessagesForChat =
            "INSERT INTO messages_for_chat " +
            "(chat_id, message_id, sender_id, receiver_id, type, status, data) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?)";

        public void Prepare()
        {
            PreparedStatement _ = _messagesForChatQuery.Value;
        }

        public void AddMessage(DataMessage message)
        {
            Activity activity = _historyActivityUtility.StartAddDataMessage(_dataUtility.MessagingSession, message.MessageId);
            bool success = false;

            try
            {
                sbyte dbMessageType = _mapper.MapHistoryToDbDataType(message.DataType);
                sbyte dbMessageStatus = _mapper.MapHistoryToDbDeliveryStatus(message.Status);
                string chatID = _dataUtility.CreateChatID(message.SenderId, message.ReceiverId);

                BoundStatement query = _messagesForChatQuery.Value.Bind(
                    chatID, message.MessageId, message.SenderId, message.ReceiverId, dbMessageType, dbMessageStatus, message.Data);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                query.SetIdempotence(false);

                _dataUtility.MessagingSession.Execute(query);
                success = true;
                _logger.LogTrace("Persisted the message {0}.", message);
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Cassandra;
using CecoChat.Contracts.History;
using CecoChat.Data.History.Instrumentation;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.History.Repos
{
    public interface IChatMessageRepo
    {
        void Prepare();

        Task<IReadOnlyCollection<HistoryMessage>> GetHistory(long userID, long otherUserID, DateTime olderThan, int countLimit);

        void AddMessage(DataMessage message);

        Task SetReaction(ReactionMessage message);

        Task UnsetReaction(ReactionMessage message);
    }

    internal class ChatMessageRepo : IChatMessageRepo
    {
        private readonly ILogger _logger;
        private readonly IHistoryActivityUtility _historyActivityUtility;
        private readonly IHistoryDbContext _dbContext;
        private readonly IDataMapper _mapper;
        private readonly Lazy<PreparedStatement> _historyQuery;
        private readonly Lazy<PreparedStatement> _messagesForChatQuery;
        private readonly Lazy<PreparedStatement> _setReactionQuery;
        private readonly Lazy<PreparedStatement> _unsetReactionQuery;

        public ChatMessageRepo(
            ILogger<ChatMessageRepo> logger,
            IHistoryActivityUtility historyActivityUtility,
            IHistoryDbContext dbContext,
            IDataMapper mapper)
        {
            _logger = logger;
            _historyActivityUtility = historyActivityUtility;
            _dbContext = dbContext;
            _mapper = mapper;

            _historyQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(SelectMessagesForChat));
            _messagesForChatQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(InsertIntoMessagesForChat));
            _setReactionQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(SetReactionCommand));
            _unsetReactionQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(UnsetReactionCommand));
        }

        private const string SelectMessagesForChat =
            "SELECT message_id, sender_id, receiver_id, type, data, reactions " +
            "FROM chat_messages " +
            "WHERE chat_id = ? AND message_id < ? ORDER BY message_id DESC LIMIT ?";
        private const string InsertIntoMessagesForChat =
            "INSERT INTO chat_messages " +
            "(chat_id, message_id, sender_id, receiver_id, type, data) " +
            "VALUES (?, ?, ?, ?, ?, ?)";
        private const string SetReactionCommand =
            "UPDATE chat_messages " +
            "SET reactions[?] = ? " +
            "WHERE chat_id = ? AND message_id = ?";
        private const string UnsetReactionCommand =
            "DELETE reactions[?] " +
            "FROM chat_messages " +
            "WHERE chat_id = ? AND message_id = ?";

        public void Prepare()
        {
            PreparedStatement _ = _historyQuery.Value;
            PreparedStatement __ = _messagesForChatQuery.Value;
            PreparedStatement ___ = _setReactionQuery.Value;
            PreparedStatement ____ = _unsetReactionQuery.Value;
        }

        public async Task<IReadOnlyCollection<HistoryMessage>> GetHistory(long userID, long otherUserID, DateTime olderThan, int countLimit)
        {
            Activity activity = _historyActivityUtility.StartGetHistory(_dbContext.Session, userID);
            bool success = false;

            try
            {
                string chatID = DataUtility.CreateChatID(userID, otherUserID);
                long olderThanSnowflake = olderThan.ToSnowflakeCeiling();
                BoundStatement query = _historyQuery.Value.Bind(chatID, olderThanSnowflake, countLimit);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                query.SetIdempotence(true);

                RowSet rows = await _dbContext.Session.ExecuteAsync(query);
                List<HistoryMessage> messages = new(capacity: countLimit);
                ReadRows(rows, messages);
                success = true;

                _logger.LogTrace("Returned {0} messages between for chat {1} which are older than {2}.", messages.Count, chatID, olderThan);
                return messages;
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }

        private void ReadRows(RowSet rows, ICollection<HistoryMessage> messages)
        {
            foreach (Row row in rows)
            {
                HistoryMessage message = new();

                message.MessageId = row.GetValue<long>("message_id");
                message.SenderId = row.GetValue<long>("sender_id");
                message.ReceiverId = row.GetValue<long>("receiver_id");
                sbyte messageType = row.GetValue<sbyte>("type");
                message.DataType = _mapper.MapDbToHistoryDataType(messageType);
                message.Data = row.GetValue<string>("data");
                IDictionary<long, string> reactions = row.GetValue<IDictionary<long, string>>("reactions");
                if (reactions != null)
                {
                    message.Reactions.Add(reactions);
                }

                messages.Add(message);
            }
        }

        public void AddMessage(DataMessage message)
        {
            Activity activity = _historyActivityUtility.StartAddDataMessage(_dbContext.Session, message.MessageId);
            bool success = false;

            try
            {
                sbyte dbMessageType = _mapper.MapHistoryToDbDataType(message.DataType);
                string chatID = DataUtility.CreateChatID(message.SenderId, message.ReceiverId);

                BoundStatement query = _messagesForChatQuery.Value.Bind(
                    chatID, message.MessageId, message.SenderId, message.ReceiverId, dbMessageType, message.Data);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                query.SetIdempotence(false);

                _dbContext.Session.Execute(query);
                success = true;
                _logger.LogTrace("Persisted the message {0}.", message);
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }

        public async Task SetReaction(ReactionMessage message)
        {
            Activity activity = _historyActivityUtility.StartSetReaction(_dbContext.Session, message.ReactorId);
            bool success = false;

            try
            {
                string chatID = DataUtility.CreateChatID(message.SenderId, message.ReceiverId);
                BoundStatement query = _setReactionQuery.Value.Bind(message.ReactorId, message.Reaction, chatID, message.MessageId);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                query.SetIdempotence(false);
                await _dbContext.Session.ExecuteAsync(query);

                success = true;
                _logger.LogTrace("User {0} reacted with {1} to message {2}.", message.ReactorId, message.Reaction, message.MessageId);
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }

        public async Task UnsetReaction(ReactionMessage message)
        {
            Activity activity = _historyActivityUtility.StartUnsetReaction(_dbContext.Session, message.ReactorId);
            bool success = false;

            try
            {
                string chatID = DataUtility.CreateChatID(message.SenderId, message.ReceiverId);
                BoundStatement query = _unsetReactionQuery.Value.Bind(message.ReactorId, chatID, message.MessageId);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                query.SetIdempotence(false);
                await _dbContext.Session.ExecuteAsync(query);

                success = true;
                _logger.LogTrace("User {0} removed reaction to message {1}.", message.ReactorId, message.MessageId);
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }
    }
}
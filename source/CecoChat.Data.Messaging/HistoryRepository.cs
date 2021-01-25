using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cassandra;
using CecoChat.Contracts.Backend;
using CecoChat.ProtobufNet;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Messaging
{
    public interface IHistoryRepository
    {
        Task<IReadOnlyCollection<Message>> GetUserHistory(long userID, DateTime olderThan, int countLimit);

        Task<IReadOnlyCollection<Message>> GetDialogHistory(long userID, long otherUserID, DateTime olderThan, int countLimit);
    }

    public sealed class HistoryRepository : IHistoryRepository
    {
        private readonly ILogger _logger;
        private readonly ICecoChatDbContext _dbContext;
        private readonly Lazy<PreparedStatement> _userHistoryQuery;
        private readonly Lazy<PreparedStatement> _dialogHistoryQuery;
        private readonly ProtobufSerializer _messageSerializer;

        public HistoryRepository(
            ILogger<NewMessageRepository> logger,
            ICecoChatDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
            _userHistoryQuery = new Lazy<PreparedStatement>(PrepareUserHistoryQuery);
            _dialogHistoryQuery = new Lazy<PreparedStatement>(PrepareDialogHistoryQuery);
            _messageSerializer = new ProtobufSerializer();
        }

        private PreparedStatement PrepareUserHistoryQuery()
        {
            ISession session = _dbContext.Messaging;
            const string cql = "SELECT data FROM messages_for_user WHERE user_id = ? AND when < ? ORDER BY when DESC LIMIT ?";
            PreparedStatement preparedQuery = session.Prepare(cql);
            _logger.LogTrace("Prepared CQL '{0}'.", cql);
            return preparedQuery;
        }

        public async Task<IReadOnlyCollection<Message>> GetUserHistory(long userID, DateTime olderThan, int countLimit)
        {
            BoundStatement query = _userHistoryQuery.Value.Bind(userID, olderThan, countLimit);
            query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);

            ISession session = _dbContext.Messaging;
            RowSet rows = await session.ExecuteAsync(query);
            List<Message> messages = new(capacity: countLimit);

            // TODO: reuse reading messages
            foreach (Row row in rows)
            {
                byte[] messageBytes = row.GetValue<byte[]>("data");
                Message message = _messageSerializer.DeserializeFromSpan<Message>(messageBytes);
                messages.Add(message);
            }

            _logger.LogTrace("Returned {0} messages for user {1} older than {2}.", messages.Count, userID, olderThan);
            return messages;
        }

        private PreparedStatement PrepareDialogHistoryQuery()
        {
            ISession session = _dbContext.Messaging;
            const string cql = "SELECT data FROM messages_for_dialog WHERE dialog_id = ? AND when < ? ORDER BY when DESC LIMIT ?";
            PreparedStatement preparedQuery = session.Prepare(cql);
            _logger.LogTrace("Prepared CQL '{0}'.", cql);
            return preparedQuery;
        }

        public async Task<IReadOnlyCollection<Message>> GetDialogHistory(long userID, long otherUserID, DateTime olderThan, int countLimit)
        {
            string dialogID = CreateDialogID(userID, otherUserID);
            BoundStatement query = _dialogHistoryQuery.Value.Bind(dialogID, otherUserID, olderThan, countLimit);
            query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);

            ISession session = _dbContext.Messaging;
            RowSet rows = await session.ExecuteAsync(query);
            List<Message> messages = new(capacity: countLimit);

            foreach (Row row in rows)
            {
                byte[] messageBytes = row.GetValue<byte[]>("data");
                Message message = _messageSerializer.DeserializeFromSpan<Message>(messageBytes);
                messages.Add(message);
            }

            _logger.LogTrace("Returned {0} messages between [{1} <-> {2}] older than {3}.", messages.Count, userID, otherUserID, olderThan);
            return messages;
        }

        private static string CreateDialogID(long userID1, long userID2)
        {
            long min = Math.Min(userID1, userID2);
            long max = Math.Max(userID1, userID2);

            return $"{min}-{max}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cassandra;
using CecoChat.Contracts.Backend;
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
        private readonly IDataUtility _dataUtility;
        private readonly Lazy<PreparedStatement> _userHistoryQuery;
        private readonly Lazy<PreparedStatement> _dialogHistoryQuery;

        public HistoryRepository(
            ILogger<NewMessageRepository> logger,
            IDataUtility dataUtility)
        {
            _logger = logger;
            _dataUtility = dataUtility;

            _userHistoryQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(SelectMessagesForUser));
            _dialogHistoryQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(SelectMessagesForDialog));
        }

        const string SelectMessagesForUser = "SELECT data FROM messages_for_user WHERE user_id = ? AND when < ? ORDER BY when DESC LIMIT ?";
        const string SelectMessagesForDialog = "SELECT data FROM messages_for_dialog WHERE dialog_id = ? AND when < ? ORDER BY when DESC LIMIT ?";

        public async Task<IReadOnlyCollection<Message>> GetUserHistory(long userID, DateTime olderThan, int countLimit)
        {
            BoundStatement query = _userHistoryQuery.Value.Bind(userID, olderThan, countLimit);
            query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
            List<Message> messages = await _dataUtility.GetMessages(query, countLimit);

            _logger.LogTrace("Returned {0} messages for user {1} older than {2}.", messages.Count, userID, olderThan);
            return messages;
        }

        public async Task<IReadOnlyCollection<Message>> GetDialogHistory(long userID, long otherUserID, DateTime olderThan, int countLimit)
        {
            string dialogID = _dataUtility.CreateDialogID(userID, otherUserID);
            BoundStatement query = _dialogHistoryQuery.Value.Bind(dialogID, otherUserID, olderThan, countLimit);
            query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
            List<Message> messages = await _dataUtility.GetMessages(query, countLimit);

            _logger.LogTrace("Returned {0} messages between [{1} <-> {2}] older than {3}.", messages.Count, userID, otherUserID, olderThan);
            return messages;
        }
    }
}

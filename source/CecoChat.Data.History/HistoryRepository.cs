using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Cassandra;
using CecoChat.Contracts.History;
using CecoChat.Data.History.Instrumentation;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.History
{
    public interface IHistoryRepository
    {
        void Prepare();

        Task<IReadOnlyCollection<HistoryMessage>> GetUserHistory(long userID, DateTime olderThan, int countLimit);

        Task<IReadOnlyCollection<HistoryMessage>> GetDialogHistory(long userID, long otherUserID, DateTime olderThan, int countLimit);
    }

    internal sealed class HistoryRepository : IHistoryRepository
    {
        private readonly ILogger _logger;
        private readonly IHistoryActivityUtility _historyActivityUtility;
        private readonly IDataUtility _dataUtility;
        private readonly IMessageMapper _mapper;
        private readonly Lazy<PreparedStatement> _userHistoryQuery;
        private readonly Lazy<PreparedStatement> _dialogHistoryQuery;

        public HistoryRepository(
            ILogger<HistoryRepository> logger,
            IHistoryActivityUtility historyActivityUtility,
            IDataUtility dataUtility,
            IMessageMapper mapper)
        {
            _logger = logger;
            _historyActivityUtility = historyActivityUtility;
            _dataUtility = dataUtility;
            _mapper = mapper;

            _userHistoryQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(SelectMessagesForUser));
            _dialogHistoryQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(SelectMessagesForDialog));
        }

        private const string SelectMessagesForUser =
            "SELECT message_id, sender_id, receiver_id, type, status, data, reactions " +
            "FROM messages_for_user WHERE user_id = ? AND message_id < ? ORDER BY message_id DESC LIMIT ?";
        private const string SelectMessagesForDialog =
            "SELECT message_id, sender_id, receiver_id, type, status, data, reactions " +
            "FROM messages_for_dialog WHERE dialog_id = ? AND message_id < ? ORDER BY message_id DESC LIMIT ?";

        public void Prepare()
        {
            // preparing the queries beforehand is optional and is implemented using the lazy pattern
            PreparedStatement _ = _userHistoryQuery.Value;
            #pragma warning disable IDE0059
            PreparedStatement __ = _dialogHistoryQuery.Value;
            #pragma warning restore IDE0059
        }

        public async Task<IReadOnlyCollection<HistoryMessage>> GetUserHistory(long userID, DateTime olderThan, int countLimit)
        {
            Activity activity = _historyActivityUtility.StartGetHistory(
                HistoryInstrumentation.Operations.HistoryGetUserHistory, _dataUtility.MessagingSession, userID);
            bool success = false;

            try
            {
                long olderThanSnowflake = olderThan.ToSnowflakeCeiling();
                BoundStatement query = _userHistoryQuery.Value.Bind(userID, olderThanSnowflake, countLimit);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                query.SetIdempotence(true);
                List<HistoryMessage> messages = await GetMessages(query, countLimit);
                success = true;

                _logger.LogTrace("Returned {0} messages for user {1} older than {2}.", messages.Count, userID, olderThan);
                return messages;
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }

        public async Task<IReadOnlyCollection<HistoryMessage>> GetDialogHistory(long userID, long otherUserID, DateTime olderThan, int countLimit)
        {
            Activity activity = _historyActivityUtility.StartGetHistory(
                HistoryInstrumentation.Operations.HistoryGetDialogHistory, _dataUtility.MessagingSession, userID);
            bool success = false;

            try
            {
                string dialogID = _dataUtility.CreateDialogID(userID, otherUserID);
                long olderThanSnowflake = olderThan.ToSnowflakeCeiling();
                BoundStatement query = _dialogHistoryQuery.Value.Bind(dialogID, olderThanSnowflake, countLimit);
                query.SetIdempotence(true);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                List<HistoryMessage> messages = await GetMessages(query, countLimit);
                success = true;

                _logger.LogTrace("Returned {0} messages between [{1} <-> {2}] older than {3}.", messages.Count, userID, otherUserID, olderThan);
                return messages;
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }

        private async Task<List<HistoryMessage>> GetMessages(IStatement query, int countHint)
        {
            RowSet rows = await _dataUtility.MessagingSession.ExecuteAsync(query);
            List<HistoryMessage> messages = new(capacity: countHint);

            foreach (Row row in rows)
            {
                HistoryMessage message = new();

                message.MessageId = row.GetValue<long>("message_id");
                message.SenderId = row.GetValue<long>("sender_id");
                message.ReceiverId = row.GetValue<long>("receiver_id");
                sbyte messageType = row.GetValue<sbyte>("type");
                message.Type = _mapper.MapDbToHistoryMessageType(messageType);
                sbyte status = row.GetValue<sbyte>("status");
                message.Status = _mapper.MapDbToHistoryMessageStatus(status);
                message.Text = row.GetValue<string>("data");
                IDictionary<long, string> reactions = row.GetValue<IDictionary<long, string>>("reactions");
                if (reactions != null)
                {
                    message.Reactions.Add(reactions);
                }

                messages.Add(message);
            }

            return messages;
        }
    }
}

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
    public interface IHistoryRepo
    {
        void Prepare();

        Task<IReadOnlyCollection<HistoryMessage>> GetHistory(long userID, long otherUserID, DateTime olderThan, int countLimit);
    }

    internal sealed class HistoryRepo : IHistoryRepo
    {
        private readonly ILogger _logger;
        private readonly IHistoryActivityUtility _historyActivityUtility;
        private readonly IHistoryDbContext _dbContext;
        private readonly IDataMapper _mapper;
        private readonly Lazy<PreparedStatement> _historyQuery;

        public HistoryRepo(
            ILogger<HistoryRepo> logger,
            IHistoryActivityUtility historyActivityUtility,
            IHistoryDbContext dbContext,
            IDataMapper mapper)
        {
            _logger = logger;
            _historyActivityUtility = historyActivityUtility;
            _dbContext = dbContext;
            _mapper = mapper;

            _historyQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(SelectMessagesForChat));
        }

        private const string SelectMessagesForChat =
            "SELECT message_id, sender_id, receiver_id, type, status, data, reactions " +
            "FROM messages_for_chat WHERE chat_id = ? AND message_id < ? ORDER BY message_id DESC LIMIT ?";

        public void Prepare()
        {
            PreparedStatement _ = _historyQuery.Value;
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

                List<HistoryMessage> messages = await GetMessages(query, countLimit);
                success = true;

                _logger.LogTrace("Returned {0} messages between for chat {1} which are older than {2}.", messages.Count, chatID, olderThan);
                return messages;
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }

        private async Task<List<HistoryMessage>> GetMessages(IStatement query, int countHint)
        {
            RowSet rows = await _dbContext.Session.ExecuteAsync(query);
            List<HistoryMessage> messages = new(capacity: countHint);

            foreach (Row row in rows)
            {
                HistoryMessage message = new();

                message.MessageId = row.GetValue<long>("message_id");
                message.SenderId = row.GetValue<long>("sender_id");
                message.ReceiverId = row.GetValue<long>("receiver_id");
                sbyte messageType = row.GetValue<sbyte>("type");
                message.DataType = _mapper.MapDbToHistoryDataType(messageType);
                message.Data = row.GetValue<string>("data");
                sbyte status = row.GetValue<sbyte>("status");
                message.Status = _mapper.MapDbToHistoryDeliveryStatus(status);
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

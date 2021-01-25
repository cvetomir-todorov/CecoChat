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
    }

    public sealed class HistoryRepository : IHistoryRepository
    {
        private readonly ILogger _logger;
        private readonly ICecoChatDbContext _dbContext;
        private readonly Lazy<PreparedStatement> _selectPrepared;
        private readonly GenericSerializer<Message> _messageSerializer;

        public HistoryRepository(
            ILogger<NewMessageRepository> logger,
            ICecoChatDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
            _selectPrepared = new Lazy<PreparedStatement>(CreateSelectPrepared);
            _messageSerializer = new GenericSerializer<Message>();
        }

        private PreparedStatement CreateSelectPrepared()
        {
            ISession session = _dbContext.Messaging;
            const string selectCql = "SELECT * FROM messages_for_user WHERE receiver_id = ? AND when < ? ORDER BY when DESC LIMIT ?";
            PreparedStatement selectPrepared = session.Prepare(selectCql);
            _logger.LogTrace("Prepared CQL '{0}'.", selectCql);
            return selectPrepared;
        }

        public async Task<IReadOnlyCollection<Message>> GetUserHistory(long userID, DateTime olderThan, int countLimit)
        {
            BoundStatement selectBound = _selectPrepared.Value.Bind(userID, olderThan, countLimit);
            selectBound.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);

            ISession session = _dbContext.Messaging;
            RowSet rows = await session.ExecuteAsync(selectBound);
            List<Message> messages = new(capacity: countLimit);

            foreach (Row row in rows)
            {
                byte[] messageBytes = row.GetValue<byte[]>("data");
                Message message = _messageSerializer.DeserializeFromSpan(messageBytes);
                messages.Add(message);
            }

            _logger.LogTrace("Returned {0} messages for user {1} older than {2}.", messages.Count, userID, olderThan);
            return messages;
        }
    }
}

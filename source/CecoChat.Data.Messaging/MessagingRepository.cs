using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cassandra;
using CecoChat.Contracts.Backend;
using CecoChat.ProtobufNet;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Messaging
{
    public interface IMessagingRepository
    {
        Task InsertMessage(Message message);

        Task<IReadOnlyCollection<Message>> SelectNewerMessagesForReceiver(long receiverID, DateTime newerThan);
    }

    public sealed class MessagingRepository : IMessagingRepository
    {
        private readonly ILogger _logger;
        private readonly ICecoChatDbContext _dbContext;
        private readonly Lazy<PreparedStatement> _insertPrepared;
        private readonly Lazy<PreparedStatement> _selectPrepared;
        private readonly GenericSerializer<Message> _messageSerializer;

        public MessagingRepository(
            ILogger<MessagingRepository> logger,
            ICecoChatDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
            _insertPrepared = new Lazy<PreparedStatement>(CreateInsertPrepared);
            _selectPrepared = new Lazy<PreparedStatement>(CreateSelectPrepared);
            _messageSerializer = new GenericSerializer<Message>();
        }

        private PreparedStatement CreateInsertPrepared()
        {
            ISession session = _dbContext.Messaging;
            const string insertCql = "INSERT INTO messages_for_user (receiver_id, when, data) VALUES (?, ?, ?)";
            PreparedStatement insertPrepared = session.Prepare(insertCql);
            _logger.LogTrace("Prepared CQL '{0}'.", insertCql);
            return insertPrepared;
        }

        public async Task InsertMessage(Message message)
        {
            byte[] messageBytes = _messageSerializer.SerializeToByteArray(message);
            BoundStatement insertBound = _insertPrepared.Value.Bind(message.ReceiverID, message.Timestamp, messageBytes);
            // TODO: read about cassandra consistencies
            insertBound.SetConsistencyLevel(ConsistencyLevel.LocalOne);

            ISession session = _dbContext.Messaging;
            await session.ExecuteAsync(insertBound);
            _logger.LogTrace("Persisted message {0}.", message);
        }

        private PreparedStatement CreateSelectPrepared()
        {
            ISession session = _dbContext.Messaging;
            const string selectCql = "SELECT * FROM messages_for_user WHERE receiver_id = ? AND when > ? ORDER BY when DESC LIMIT ?";
            PreparedStatement selectPrepared = session.Prepare(selectCql);
            _logger.LogTrace("Prepared CQL '{0}'.", selectCql);
            return selectPrepared;
        }

        public async Task<IReadOnlyCollection<Message>> SelectNewerMessagesForReceiver(long receiverID, DateTime newerThan)
        {
            // TODO: consider moving this in configuration
            const int messageCountLimit = 128;
            BoundStatement selectBound = _selectPrepared.Value.Bind(receiverID, newerThan, messageCountLimit);
            selectBound.SetConsistencyLevel(ConsistencyLevel.LocalOne);

            ISession session = _dbContext.Messaging;
            RowSet rows = await session.ExecuteAsync(selectBound);
            List<Message> messages = new(capacity: 32);

            foreach (Row row in rows)
            {
                byte[] messageBytes = row.GetValue<byte[]>("data");
                Message message = _messageSerializer.DeserializeFromSpan(messageBytes);
                messages.Add(message);
            }

            _logger.LogTrace("Returned {0} messages for user {1} newer than {2}.", messages.Count, receiverID, newerThan);
            return messages;
        }
    }
}

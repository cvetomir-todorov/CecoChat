using System;
using System.Threading.Tasks;
using Cassandra;
using CecoChat.Contracts.Backend;
using CecoChat.ProtobufNet;
using Microsoft.Extensions.Logging;

namespace CecoChat.Materialize.Server.Database
{
    public interface IMessagingRepository
    {
        Task InsertMessage(Message message);
    }

    public sealed class MessagingRepository : IMessagingRepository
    {
        private readonly ILogger _logger;
        private readonly ICecoChatDbContext _dbContext;
        private readonly Lazy<PreparedStatement> _insertPrepared;
        private readonly GenericSerializer<Message> _messageSerializer;

        public MessagingRepository(
            ILogger<MessagingRepository> logger,
            ICecoChatDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
            _insertPrepared = new Lazy<PreparedStatement>(CreateInsertPrepared);
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
            insertBound.SetConsistencyLevel(ConsistencyLevel.Two);

            ISession session = _dbContext.Messaging;
            await session.ExecuteAsync(insertBound);
            _logger.LogTrace("Persisted message {0}.", message);
        }
    }
}

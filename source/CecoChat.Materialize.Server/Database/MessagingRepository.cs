using System;
using System.Buffers;
using System.Threading.Tasks;
using Cassandra;
using CecoChat.Contracts.Backend;
using Microsoft.Extensions.Logging;
using ProtoBuf;

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

        public MessagingRepository(
            ILogger<MessagingRepository> logger,
            ICecoChatDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
            _insertPrepared = new Lazy<PreparedStatement>(CreateInsertPrepared);
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
            // TODO: reuse message serialization with kafka serializers
            ArrayBufferWriter<byte> array = new ArrayBufferWriter<byte>(initialCapacity: 128);
            Serializer.Serialize(array, message);
            byte[] data = array.WrittenMemory.ToArray();

            // TODO: convert IDs to long
            BoundStatement insertBound = _insertPrepared.Value.Bind((long)message.ReceiverID, message.Timestamp, data);
            insertBound.SetConsistencyLevel(ConsistencyLevel.Two);

            ISession session = _dbContext.Messaging;
            await session.ExecuteAsync(insertBound);
            _logger.LogTrace("Persisted message {0}.", message);
        }
    }
}

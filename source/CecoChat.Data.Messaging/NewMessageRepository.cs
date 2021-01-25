using System;
using Cassandra;
using CecoChat.Contracts.Backend;
using CecoChat.ProtobufNet;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Messaging
{
    public interface INewMessageRepository
    {
        void AddNewDialogMessage(Message message);
    }

    public sealed class NewMessageRepository : INewMessageRepository
    {
        private readonly ILogger _logger;
        private readonly ICecoChatDbContext _dbContext;
        private readonly Lazy<PreparedStatement> _messagesForUserQuery;
        private readonly Lazy<PreparedStatement> _messagesForDialogQuery;
        private readonly GenericSerializer<Message> _messageSerializer;

        public NewMessageRepository(
            ILogger<NewMessageRepository> logger,
            ICecoChatDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
            _messagesForUserQuery = new Lazy<PreparedStatement>(PrepareMessagesForUserQuery);
            _messagesForDialogQuery = new Lazy<PreparedStatement>(PrepareMessagesForDialogQuery);
            _messageSerializer = new GenericSerializer<Message>();
        }

        // TODO: reuse prepare query
        private PreparedStatement PrepareMessagesForUserQuery()
        {
            ISession session = _dbContext.Messaging;
            const string cql = "INSERT INTO messages_for_user (user_id, when, data) VALUES (?, ?, ?)";
            PreparedStatement preparedQuery = session.Prepare(cql);
            _logger.LogTrace("Prepared CQL '{0}'.", cql);
            return preparedQuery;
        }

        private PreparedStatement PrepareMessagesForDialogQuery()
        {
            ISession session = _dbContext.Messaging;
            const string cql = "INSERT INTO messages_for_dialog (dialog_id, when, data) VALUES (?, ?, ?)";
            PreparedStatement preparedQuery = session.Prepare(cql);
            _logger.LogTrace("Prepared CQL '{0}'.", cql);
            return preparedQuery;
        }

        public void AddNewDialogMessage(Message message)
        {
            byte[] messageBytes = _messageSerializer.SerializeToByteArray(message);
            BoundStatement insertForSender = _messagesForUserQuery.Value.Bind(message.SenderID, message.Timestamp, messageBytes);
            BoundStatement insertForReceiver = _messagesForUserQuery.Value.Bind(message.ReceiverID, message.Timestamp, messageBytes);

            string dialogID = CreateDialogID(message.SenderID, message.ReceiverID);
            BoundStatement insertForDialog = _messagesForDialogQuery.Value.Bind(dialogID, message.Timestamp, messageBytes);

            BatchStatement insertBatch = new BatchStatement()
                .Add(insertForSender)
                .Add(insertForReceiver)
                .Add(insertForDialog);
            insertBatch.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);

            ISession session = _dbContext.Messaging;
            session.Execute(insertBatch);
            _logger.LogTrace("Persisted dialog message {0}.", message);
        }

        // TODO: reuse creating dialog ID
        private static string CreateDialogID(long userID1, long userID2)
        {
            long min = Math.Min(userID1, userID2);
            long max = Math.Max(userID1, userID2);

            return $"{min}-{max}";
        }
    }
}

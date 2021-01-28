using System;
using Cassandra;
using CecoChat.Contracts.Backend;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Messaging
{
    public interface INewMessageRepository
    {
        void AddNewDialogMessage(BackendMessage message);
    }

    public sealed class NewMessageRepository : INewMessageRepository
    {
        private readonly ILogger _logger;
        private readonly ICecoChatDbContext _dbContext;
        private readonly IDataUtility _dataUtility;
        private readonly Lazy<PreparedStatement> _messagesForUserQuery;
        private readonly Lazy<PreparedStatement> _messagesForDialogQuery;

        public NewMessageRepository(
            ILogger<NewMessageRepository> logger,
            ICecoChatDbContext dbContext,
            IDataUtility dataUtility)
        {
            _logger = logger;
            _dbContext = dbContext;
            _dataUtility = dataUtility;

            _messagesForUserQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(InsertIntoMessagesForUser));
            _messagesForDialogQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(InsertIntoMessagesForDialog));
        }

        const string InsertIntoMessagesForUser = "INSERT INTO messages_for_user (user_id, when, data) VALUES (?, ?, ?)";
        const string InsertIntoMessagesForDialog = "INSERT INTO messages_for_dialog (dialog_id, when, data) VALUES (?, ?, ?)";

        public void AddNewDialogMessage(BackendMessage message)
        {
            byte[] messageBytes = message.ToByteArray();
            DateTime messageTimestamp = message.Timestamp.ToDateTime();
            BoundStatement insertForSender = _messagesForUserQuery.Value.Bind(message.SenderId, messageTimestamp, messageBytes);
            BoundStatement insertForReceiver = _messagesForUserQuery.Value.Bind(message.ReceiverId, messageTimestamp, messageBytes);

            string dialogID = _dataUtility.CreateDialogID(message.SenderId, message.ReceiverId);
            BoundStatement insertForDialog = _messagesForDialogQuery.Value.Bind(dialogID, messageTimestamp, messageBytes);

            BatchStatement insertBatch = new BatchStatement()
                .Add(insertForSender)
                .Add(insertForReceiver)
                .Add(insertForDialog);
            insertBatch.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);

            ISession session = _dbContext.Messaging;
            session.Execute(insertBatch);
            _logger.LogTrace("Persisted dialog message {0}.", message);
        }
    }
}

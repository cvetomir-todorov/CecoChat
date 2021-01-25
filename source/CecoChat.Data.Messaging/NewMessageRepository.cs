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
        private readonly IDataUtility _dataUtility;
        private readonly Lazy<PreparedStatement> _messagesForUserQuery;
        private readonly Lazy<PreparedStatement> _messagesForDialogQuery;
        private readonly ProtobufSerializer _messageSerializer;

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
            _messageSerializer = new ProtobufSerializer();
        }

        const string InsertIntoMessagesForUser = "INSERT INTO messages_for_user (user_id, when, data) VALUES (?, ?, ?)";
        const string InsertIntoMessagesForDialog = "INSERT INTO messages_for_dialog (dialog_id, when, data) VALUES (?, ?, ?)";

        public void AddNewDialogMessage(Message message)
        {
            byte[] messageBytes = _messageSerializer.SerializeToByteArray(message);
            BoundStatement insertForSender = _messagesForUserQuery.Value.Bind(message.SenderID, message.Timestamp, messageBytes);
            BoundStatement insertForReceiver = _messagesForUserQuery.Value.Bind(message.ReceiverID, message.Timestamp, messageBytes);

            string dialogID = _dataUtility.CreateDialogID(message.SenderID, message.ReceiverID);
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
    }
}

using System;
using System.Diagnostics;
using Cassandra;
using CecoChat.Contracts.History;
using CecoChat.Data.History.Instrumentation;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.History.Repos
{
    public interface INewMessageRepo
    {
        void Prepare();

        void AddMessage(DataMessage message);
    }

    internal sealed class NewMessageRepo : INewMessageRepo
    {
        private readonly ILogger _logger;
        private readonly IHistoryActivityUtility _historyActivityUtility;
        private readonly IDataUtility _dataUtility;
        private readonly IDataMapper _mapper;
        private readonly Lazy<PreparedStatement> _messagesForUserQuery;
        private readonly Lazy<PreparedStatement> _messagesForDialogQuery;

        public NewMessageRepo(
            ILogger<NewMessageRepo> logger,
            IHistoryActivityUtility historyActivityUtility,
            IDataUtility dataUtility,
            IDataMapper mapper)
        {
            _logger = logger;
            _historyActivityUtility = historyActivityUtility;
            _dataUtility = dataUtility;
            _mapper = mapper;

            _messagesForUserQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(InsertIntoMessagesForUser));
            _messagesForDialogQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(InsertIntoMessagesForDialog));
        }

        private const string InsertIntoMessagesForUser =
            "INSERT INTO messages_for_user " +
            "(user_id, message_id, sender_id, receiver_id, type, status, data) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?)";
        private const string InsertIntoMessagesForDialog =
            "INSERT INTO messages_for_dialog " +
            "(dialog_id, message_id, sender_id, receiver_id, type, status, data) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?)";

        public void Prepare()
        {
            // preparing the queries beforehand is optional and is implemented using the lazy pattern
            PreparedStatement _ = _messagesForUserQuery.Value;
            #pragma warning disable IDE0059
            PreparedStatement __ = _messagesForDialogQuery.Value;
            #pragma warning restore IDE0059
        }

        public void AddMessage(DataMessage message)
        {
            Activity activity = _historyActivityUtility.StartAddDataMessage(_dataUtility.MessagingSession, message.MessageId);
            bool success = false;

            try
            {
                BatchStatement insertBatch = CreateInsertBatch(message);
                _dataUtility.MessagingSession.Execute(insertBatch);
                success = true;
                _logger.LogTrace("Persisted for sender, receiver and dialog the message {0}.", message);
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }

        private BatchStatement CreateInsertBatch(DataMessage message)
        {
            long senderID = message.SenderId;
            long receiverID = message.ReceiverId;
            sbyte dbMessageType = _mapper.MapHistoryToDbDataType(message.DataType);
            sbyte dbMessageStatus = _mapper.MapHistoryToDbDeliveryStatus(message.Status);
            string data = message.Data;
            string dialogID = _dataUtility.CreateDialogID(senderID, receiverID);

            BoundStatement insertForSender = _messagesForUserQuery.Value.Bind(
                senderID, message.MessageId, senderID, receiverID, dbMessageType, dbMessageStatus, data);
            BoundStatement insertForReceiver = _messagesForUserQuery.Value.Bind(
                receiverID, message.MessageId, senderID, receiverID, dbMessageType, dbMessageStatus, data);
            BoundStatement insertForDialog = _messagesForDialogQuery.Value.Bind(
                dialogID, message.MessageId, senderID, receiverID, dbMessageType, dbMessageStatus, data);

            BatchStatement insertBatch = new BatchStatement()
                .Add(insertForSender)
                .Add(insertForReceiver)
                .Add(insertForDialog);
            insertBatch.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
            insertBatch.SetIdempotence(false);
            return insertBatch;
        }
    }
}

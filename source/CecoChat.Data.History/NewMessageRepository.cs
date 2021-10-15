using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cassandra;
using CecoChat.Contracts.History;
using CecoChat.Data.History.Instrumentation;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.History
{
    public interface INewMessageRepository
    {
        void Prepare();

        void AddNewDialogMessage(HistoryMessage message);
    }

    internal sealed class NewMessageRepository : INewMessageRepository
    {
        private readonly ILogger _logger;
        private readonly IHistoryActivityUtility _historyActivityUtility;
        private readonly IDataUtility _dataUtility;
        private readonly IMessageMapper _mapper;
        private readonly Lazy<PreparedStatement> _messagesForUserQuery;
        private readonly Lazy<PreparedStatement> _messagesForDialogQuery;

        public NewMessageRepository(
            ILogger<NewMessageRepository> logger,
            IHistoryActivityUtility historyActivityUtility,
            IDataUtility dataUtility,
            IMessageMapper mapper)
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
            "(user_id, message_id, sender_id, receiver_id, message_type, status, data) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?)";
        private const string InsertIntoMessagesForDialog =
            "INSERT INTO messages_for_dialog " +
            "(dialog_id, message_id, sender_id, receiver_id, message_type, status, data) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?)";

        public void Prepare()
        {
            // preparing the queries beforehand is optional and is implemented using the lazy pattern
            PreparedStatement _ = _messagesForUserQuery.Value;
            #pragma warning disable IDE0059
            PreparedStatement __ = _messagesForDialogQuery.Value;
            #pragma warning restore IDE0059
        }

        public void AddNewDialogMessage(HistoryMessage message)
        {
            Activity activity = _historyActivityUtility.StartNewDialogMessage(_dataUtility.MessagingSession, message.MessageId);
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

        private BatchStatement CreateInsertBatch(HistoryMessage message)
        {
            long senderID = message.SenderId;
            long receiverID = message.ReceiverId;
            sbyte dbMessageType = _mapper.MapHistoryToDbMessageType(message.Type);
            sbyte dbMessageStatus = _mapper.MapHistoryToDbMessageStatus(message.Status);
            IDictionary<string, string> data = _mapper.MapHistoryToDbData(message);
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

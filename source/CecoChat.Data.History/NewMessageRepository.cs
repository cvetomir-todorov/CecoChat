﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cassandra;
using CecoChat.Contracts;
using CecoChat.Contracts.Backend;
using CecoChat.Data.History.Instrumentation;
using CecoChat.Tracing;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.History
{
    public interface INewMessageRepository
    {
        void Prepare();

        void AddNewDialogMessage(BackendMessage message);
    }

    public sealed class NewMessageRepository : INewMessageRepository
    {
        private readonly ILogger _logger;
        private readonly IActivityUtility _activityUtility;
        private readonly IDataUtility _dataUtility;
        private readonly IBackendDbMapper _mapper;
        private readonly Lazy<PreparedStatement> _messagesForUserQuery;
        private readonly Lazy<PreparedStatement> _messagesForDialogQuery;

        public NewMessageRepository(
            ILogger<NewMessageRepository> logger,
            IActivityUtility activityUtility,
            IDataUtility dataUtility,
            IBackendDbMapper mapper)
        {
            _logger = logger;
            _activityUtility = activityUtility;
            _dataUtility = dataUtility;
            _mapper = mapper;

            _messagesForUserQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(InsertIntoMessagesForUser));
            _messagesForDialogQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(InsertIntoMessagesForDialog));
        }

        private const string InsertIntoMessagesForUser =
            "INSERT INTO messages_for_user " +
            "(user_id, message_id, sender_id, receiver_id, when, message_type, data) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?)";
        private const string InsertIntoMessagesForDialog =
            "INSERT INTO messages_for_dialog " +
            "(dialog_id, message_id, sender_id, receiver_id, when, message_type, data) " +
            "VALUES (?, ?, ?, ?, ?, ?, ?)";

        public void Prepare()
        {
            // preparing the queries beforehand is optional and is implemented using the lazy pattern
            PreparedStatement _ = _messagesForUserQuery.Value;
            #pragma warning disable IDE0059
            PreparedStatement __ = _messagesForDialogQuery.Value;
            #pragma warning restore IDE0059
        }

        public void AddNewDialogMessage(BackendMessage message)
        {
            Activity activity = StartActivity(message, _dataUtility.MessagingSession);
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
                _activityUtility.Stop(activity, success);
            }
        }

        private Activity StartActivity(BackendMessage message, ISession session)
        {
            Activity activity = _dataUtility.StartActivity(HistoryInstrumentation.Operations.HistoryNewDialogMessage, session);
            if (activity != null && activity.IsAllDataRequested)
            {
                activity.SetTag(HistoryInstrumentation.Keys.DbOperation, HistoryInstrumentation.Values.DbOperationBatchWrite);
                activity.SetTag("message.id", message.MessageId.ToGuid());
            }

            return activity;
        }

        private BatchStatement CreateInsertBatch(BackendMessage message)
        {
            Guid messageID = message.MessageId.ToGuid();
            DateTime messageTimestamp = message.Timestamp.ToDateTime();
            sbyte dbMessageType = _mapper.MapBackendToDbMessageType(message.Type);
            IDictionary<string, string> data = _mapper.MapBackendToDbData(message);
            string dialogID = _dataUtility.CreateDialogID(message.SenderId, message.ReceiverId);

            BoundStatement insertForSender = _messagesForUserQuery.Value.Bind(
                message.SenderId, messageID, message.SenderId, message.ReceiverId, messageTimestamp, dbMessageType, data);
            BoundStatement insertForReceiver = _messagesForUserQuery.Value.Bind(
                message.ReceiverId, messageID, message.SenderId, message.ReceiverId, messageTimestamp, dbMessageType, data);
            BoundStatement insertForDialog = _messagesForDialogQuery.Value.Bind(
                dialogID, messageID, message.SenderId, message.ReceiverId, messageTimestamp, dbMessageType, data);

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

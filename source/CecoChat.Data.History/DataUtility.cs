using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Cassandra;
using CecoChat.Contracts;
using CecoChat.Contracts.Backend;
using CecoChat.Data.History.Instrumentation;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.History
{
    public interface IDataUtility
    {
        ISession MessagingSession { get; }

        PreparedStatement PrepareQuery(string cql);

        Task<List<BackendMessage>> GetMessages(IStatement query, int countHint);

        string CreateDialogID(long userID1, long userID2);

        Activity StartActivity(string name, ISession session);
    }

    public sealed class DataUtility : IDataUtility
    {
        private readonly ILogger _logger;
        private readonly ICecoChatDbContext _dbContext;
        private readonly IBackendDbMapper _mapper;

        public DataUtility(
            ILogger<DataUtility> logger,
            ICecoChatDbContext dbContext,
            IBackendDbMapper mapper)
        {
            _logger = logger;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public ISession MessagingSession => _dbContext.Messaging;

        public PreparedStatement PrepareQuery(string cql)
        {
            PreparedStatement preparedQuery = _dbContext.Messaging.Prepare(cql);
            _logger.LogDebug("Prepared CQL '{0}'.", cql);
            return preparedQuery;
        }

        public async Task<List<BackendMessage>> GetMessages(IStatement query, int countHint)
        {
            RowSet rows = await _dbContext.Messaging.ExecuteAsync(query);
            List<BackendMessage> messages = new(capacity: countHint);

            foreach (Row row in rows)
            {
                BackendMessage message = new();

                Guid messageID = row.GetValue<Guid>("message_id");
                message.MessageId = messageID.ToUuid();
                message.SenderId = row.GetValue<long>("sender_id");
                message.ReceiverId = row.GetValue<long>("receiver_id");
                message.Timestamp = Timestamp.FromDateTimeOffset(row.GetValue<DateTimeOffset>("when"));
                message.Type = _mapper.MapDbToBackendMessageType(row.GetValue<sbyte>("message_type"));
                IDictionary<string, string> data = row.GetValue<IDictionary<string, string>>("data");
                _mapper.MapDbToBackendData(data, message);

                messages.Add(message);
            }

            return messages;
        }

        public string CreateDialogID(long userID1, long userID2)
        {
            long min = Math.Min(userID1, userID2);
            long max = Math.Max(userID1, userID2);

            return $"{min}-{max}";
        }

        public Activity StartActivity(string name, ISession session)
        {
            Activity activity = HistoryInstrumentation.ActivitySource.StartActivity(name, ActivityKind.Client);
            if (activity == null)
            {
                return null;
            }

            if (activity.IsAllDataRequested)
            {
                activity.SetTag(HistoryInstrumentation.Keys.DbSystem, HistoryInstrumentation.Values.DbSystemCassandra);
                activity.SetTag(HistoryInstrumentation.Keys.DbName, session.Keyspace);
                activity.SetTag(HistoryInstrumentation.Keys.DbSessionName, session.SessionName);
            }

            return activity;
        }
    }
}

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Cassandra;
using CecoChat.Data.History.Instrumentation;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.History
{
    public interface IReactionRepository
    {
        void Prepare();

        Task AddReaction(long messageID, long senderID, long receiverID, long reactorID, string reaction);

        Task RemoveReaction(long messageID, long senderID, long receiverID, long reactorID);
    }

    internal class ReactionRepository : IReactionRepository
    {
        private readonly ILogger _logger;
        private readonly IHistoryActivityUtility _historyActivityUtility;
        private readonly IDataUtility _dataUtility;
        private readonly Lazy<PreparedStatement> _addReactionQuery;
        private readonly Lazy<PreparedStatement> _removeReactionQuery;

        public ReactionRepository(
            ILogger<ReactionRepository> logger,
            IHistoryActivityUtility historyActivityUtility,
            IDataUtility dataUtility)
        {
            _logger = logger;
            _historyActivityUtility = historyActivityUtility;
            _dataUtility = dataUtility;

            _addReactionQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(AddReactionCommand));
            _removeReactionQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(RemoveReactionCommand));
        }

        private const string AddReactionCommand =
            "UPDATE messages_for_dialog SET reactions[?] = ? WHERE dialog_id = ? AND message_id = ?";
        private const string RemoveReactionCommand =
            "DELETE reactions[?] FROM messages_for_dialog WHERE dialog_id = ? AND message_id = ?";

        public void Prepare()
        {
            PreparedStatement _ = _addReactionQuery.Value;
            #pragma warning disable IDE0059
            PreparedStatement __ = _removeReactionQuery.Value;
            #pragma warning restore IDE0059
        }

        public async Task AddReaction(long messageID, long senderID, long receiverID, long reactorID, string reaction)
        {
            Activity activity = _historyActivityUtility.StartAddReaction(_dataUtility.MessagingSession, reactorID);
            bool success = false;

            try
            {
                string dialogID = _dataUtility.CreateDialogID(senderID, receiverID);
                BoundStatement query = _addReactionQuery.Value.Bind(reactorID, reaction, dialogID, messageID);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                query.SetIdempotence(false);
                await _dataUtility.MessagingSession.ExecuteAsync(query);
                success = true;
                _logger.LogTrace("User {0} reacted with {1} to message {2}.", reactorID, reaction, messageID);
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }

        public async Task RemoveReaction(long messageID, long senderID, long receiverID, long reactorID)
        {
            Activity activity = _historyActivityUtility.StartRemoveReaction(_dataUtility.MessagingSession, reactorID);
            bool success = false;

            try
            {
                string dialogID = _dataUtility.CreateDialogID(senderID, receiverID);
                BoundStatement query = _removeReactionQuery.Value.Bind(reactorID, dialogID, messageID);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                query.SetIdempotence(false);
                await _dataUtility.MessagingSession.ExecuteAsync(query);
                success = true;
                _logger.LogTrace("User {0} removed reaction to message {1}.", reactorID, messageID);
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }
    }
}
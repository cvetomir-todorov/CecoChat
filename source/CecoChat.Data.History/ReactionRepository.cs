using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Cassandra;
using CecoChat.Contracts.History;
using CecoChat.Data.History.Instrumentation;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.History
{
    public interface IReactionRepository
    {
        void Prepare();

        Task SetReaction(ReactionMessage message);

        Task UnsetReaction(ReactionMessage message);
    }

    internal class ReactionRepository : IReactionRepository
    {
        private readonly ILogger _logger;
        private readonly IHistoryActivityUtility _historyActivityUtility;
        private readonly IDataUtility _dataUtility;
        private readonly Lazy<PreparedStatement> _setReactionQuery;
        private readonly Lazy<PreparedStatement> _unsetReactionQuery;

        public ReactionRepository(
            ILogger<ReactionRepository> logger,
            IHistoryActivityUtility historyActivityUtility,
            IDataUtility dataUtility)
        {
            _logger = logger;
            _historyActivityUtility = historyActivityUtility;
            _dataUtility = dataUtility;

            _setReactionQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(SetReactionCommand));
            _unsetReactionQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(UnsetReactionCommand));
        }

        private const string SetReactionCommand =
            "UPDATE messages_for_dialog SET reactions[?] = ? WHERE dialog_id = ? AND message_id = ?";
        private const string UnsetReactionCommand =
            "DELETE reactions[?] FROM messages_for_dialog WHERE dialog_id = ? AND message_id = ?";

        public void Prepare()
        {
            PreparedStatement _ = _setReactionQuery.Value;
            #pragma warning disable IDE0059
            PreparedStatement __ = _unsetReactionQuery.Value;
            #pragma warning restore IDE0059
        }

        public async Task SetReaction(ReactionMessage message)
        {
            Activity activity = _historyActivityUtility.StartSetReaction(_dataUtility.MessagingSession, message.ReactorId);
            bool success = false;

            try
            {
                string dialogID = _dataUtility.CreateDialogID(message.SenderId, message.ReceiverId);
                BoundStatement query = _setReactionQuery.Value.Bind(message.ReactorId, message.Reaction, dialogID, message.MessageId);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                query.SetIdempotence(false);
                await _dataUtility.MessagingSession.ExecuteAsync(query);
                success = true;
                _logger.LogTrace("User {0} reacted with {1} to message {2}.", message.ReactorId, message.Reaction, message.MessageId);
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }

        public async Task UnsetReaction(ReactionMessage message)
        {
            Activity activity = _historyActivityUtility.StartUnsetReaction(_dataUtility.MessagingSession, message.ReactorId);
            bool success = false;

            try
            {
                string dialogID = _dataUtility.CreateDialogID(message.SenderId, message.ReceiverId);
                BoundStatement query = _unsetReactionQuery.Value.Bind(message.ReactorId, dialogID, message.MessageId);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                query.SetIdempotence(false);
                await _dataUtility.MessagingSession.ExecuteAsync(query);
                success = true;
                _logger.LogTrace("User {0} removed reaction to message {1}.", message.ReactorId, message.MessageId);
            }
            finally
            {
                _historyActivityUtility.Stop(activity, success);
            }
        }
    }
}
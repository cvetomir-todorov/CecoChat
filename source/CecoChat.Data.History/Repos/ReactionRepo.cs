using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Cassandra;
using CecoChat.Contracts.History;
using CecoChat.Data.History.Instrumentation;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.History.Repos
{
    public interface IReactionRepo
    {
        void Prepare();

        Task SetReaction(ReactionMessage message);

        Task UnsetReaction(ReactionMessage message);
    }

    internal class ReactionRepo : IReactionRepo
    {
        private readonly ILogger _logger;
        private readonly IHistoryActivityUtility _historyActivityUtility;
        private readonly IDataUtility _dataUtility;
        private readonly Lazy<PreparedStatement> _setReactionQuery;
        private readonly Lazy<PreparedStatement> _setReactionQueryForUser;
        private readonly Lazy<PreparedStatement> _unsetReactionQuery;
        private readonly Lazy<PreparedStatement> _unsetReactionQueryForUser;

        public ReactionRepo(
            ILogger<ReactionRepo> logger,
            IHistoryActivityUtility historyActivityUtility,
            IDataUtility dataUtility)
        {
            _logger = logger;
            _historyActivityUtility = historyActivityUtility;
            _dataUtility = dataUtility;

            _setReactionQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(SetReactionCommand));
            _setReactionQueryForUser = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(SetReactionCommandForUser));
            _unsetReactionQuery = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(UnsetReactionCommand));
            _unsetReactionQueryForUser = new Lazy<PreparedStatement>(() => _dataUtility.PrepareQuery(UnsetReactionCommandForUser));
        }

        private const string SetReactionCommand =
            "UPDATE messages_for_dialog SET reactions[?] = ? WHERE dialog_id = ? AND message_id = ?";
        private const string SetReactionCommandForUser =
            "UPDATE messages_for_user SET reactions[?] = ? WHERE user_id = ? AND message_id = ?";
        private const string UnsetReactionCommand =
            "DELETE reactions[?] FROM messages_for_dialog WHERE dialog_id = ? AND message_id = ?";
        private const string UnsetReactionCommandForUser =
            "DELETE reactions[?] FROM messages_for_user WHERE user_id = ? AND message_id = ?";

        public void Prepare()
        {
            PreparedStatement _ = _setReactionQuery.Value;
            #pragma warning disable IDE0059
            PreparedStatement __ = _unsetReactionQuery.Value;
            PreparedStatement ___ = _setReactionQueryForUser.Value;
            PreparedStatement ____ = _unsetReactionQueryForUser.Value;
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

                BoundStatement senderQuery = _setReactionQueryForUser.Value.Bind(message.ReactorId, message.Reaction, message.SenderId, message.MessageId);
                await _dataUtility.MessagingSession.ExecuteAsync(senderQuery);
                BoundStatement receiverQuery = _setReactionQueryForUser.Value.Bind(message.ReactorId, message.Reaction, message.ReceiverId, message.MessageId);
                await _dataUtility.MessagingSession.ExecuteAsync(receiverQuery);

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

                BoundStatement senderQuery = _unsetReactionQueryForUser.Value.Bind(message.ReactorId, message.SenderId, message.MessageId);
                await _dataUtility.MessagingSession.ExecuteAsync(senderQuery);
                BoundStatement receiverQuery = _unsetReactionQueryForUser.Value.Bind(message.ReactorId, message.ReceiverId, message.MessageId);
                await _dataUtility.MessagingSession.ExecuteAsync(receiverQuery);

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
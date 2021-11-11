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
        private readonly IHistoryDbContext _dbContext;
        private readonly Lazy<PreparedStatement> _setReactionQuery;
        private readonly Lazy<PreparedStatement> _unsetReactionQuery;

        public ReactionRepo(
            ILogger<ReactionRepo> logger,
            IHistoryActivityUtility historyActivityUtility,
            IHistoryDbContext dbContext)
        {
            _logger = logger;
            _historyActivityUtility = historyActivityUtility;
            _dbContext = dbContext;

            _setReactionQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(SetReactionCommand));
            _unsetReactionQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(UnsetReactionCommand));
        }

        private const string SetReactionCommand =
            "UPDATE chat_messages " +
            "SET reactions[?] = ? " +
            "WHERE chat_id = ? AND message_id = ?";
        private const string UnsetReactionCommand =
            "DELETE reactions[?] " +
            "FROM chat_messages " +
            "WHERE chat_id = ? AND message_id = ?";

        public void Prepare()
        {
            PreparedStatement _ = _setReactionQuery.Value;
            #pragma warning disable IDE0059
            PreparedStatement __ = _unsetReactionQuery.Value;
            #pragma warning restore IDE0059
        }

        public async Task SetReaction(ReactionMessage message)
        {
            Activity activity = _historyActivityUtility.StartSetReaction(_dbContext.Session, message.ReactorId);
            bool success = false;

            try
            {
                string chatID = DataUtility.CreateChatID(message.SenderId, message.ReceiverId);
                BoundStatement query = _setReactionQuery.Value.Bind(message.ReactorId, message.Reaction, chatID, message.MessageId);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                query.SetIdempotence(false);
                await _dbContext.Session.ExecuteAsync(query);

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
            Activity activity = _historyActivityUtility.StartUnsetReaction(_dbContext.Session, message.ReactorId);
            bool success = false;

            try
            {
                string chatID = DataUtility.CreateChatID(message.SenderId, message.ReceiverId);
                BoundStatement query = _unsetReactionQuery.Value.Bind(message.ReactorId, chatID, message.MessageId);
                query.SetConsistencyLevel(ConsistencyLevel.LocalQuorum);
                query.SetIdempotence(false);
                await _dbContext.Session.ExecuteAsync(query);

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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cassandra;
using CecoChat.Contracts.State;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.State.Repos
{
    public interface IChatStateRepo
    {
        void Prepare();
        
        Task<IReadOnlyCollection<ChatState>> GetChats(long userID, DateTime newerThan);
    }

    internal class ChatStateRepo : IChatStateRepo
    {
        private readonly ILogger _logger;
        private readonly IStateDbContext _dbContext;
        private readonly Lazy<PreparedStatement> _chatsQuery;

        public ChatStateRepo(
            ILogger<ChatStateRepo> logger,
            IStateDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;

            _chatsQuery = new Lazy<PreparedStatement>(() => _dbContext.PrepareQuery(SelectNewerChatsForUser));
        }

        private const string SelectNewerChatsForUser =
            "SELECT chat_id, newest_message, other_user_delivered, other_user_seen " +
            "FROM user_chats " +
            "WHERE user_id = ? AND newest_message > ? ALLOW FILTERING";

        public void Prepare()
        {
            PreparedStatement _ = _chatsQuery.Value;
        }

        public async Task<IReadOnlyCollection<ChatState>> GetChats(long userID, DateTime newerThan)
        {
            // TODO: add tracing

            long newerThanSnowflake = newerThan.ToSnowflakeFloor();
            BoundStatement query = _chatsQuery.Value.Bind(userID, newerThanSnowflake);
            RowSet rows = await _dbContext.Session.ExecuteAsync(query);
            List<ChatState> chats = new();

            foreach (Row row in rows)
            {
                ChatState chat = new();

                chat.ChatId = row.GetValue<string>("chat_id");
                chat.NewestMessage = row.GetValue<long>("newest_message");
                chat.OtherUserDelivered = row.GetValue<long>("other_user_delivered");
                chat.OtherUserSeen = row.GetValue<long>("other_user_seen");

                chats.Add(chat);
            }

            _logger.LogTrace("Returned {0} chats for user {1} which are newer than {2}.", chats.Count, userID, newerThan);
            return chats;
        }
    }
}
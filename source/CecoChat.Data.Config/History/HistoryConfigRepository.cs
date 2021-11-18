using System.Threading.Tasks;
using CecoChat.Redis;
using StackExchange.Redis;

namespace CecoChat.Data.Config.History
{
    internal interface IHistoryConfigRepository
    {
        Task<HistoryConfigValues> GetValues(HistoryConfigUsage usage);
    }

    internal sealed class HistoryConfigRepository : IHistoryConfigRepository
    {
        private readonly IRedisContext _redisContext;

        public HistoryConfigRepository(
            IRedisContext redisContext)
        {
            _redisContext = redisContext;
        }

        public async Task<HistoryConfigValues> GetValues(HistoryConfigUsage usage)
        {
            HistoryConfigValues values = new();

            if (usage.UseMessageCount)
            {
                values.ChatMessageCount = await GetChatMessageCount();
            }

            return values;
        }

        private async Task<int> GetChatMessageCount()
        {
            IDatabase database = _redisContext.GetDatabase();
            RedisValue value = await database.StringGetAsync(HistoryKeys.ChatMessageCount);
            value.TryParse(out int chatMessageCount);
            return chatMessageCount;
        }
    }
}

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

            if (usage.UseServerAddress)
            {
                values.ServerAddress = await GetServerAddress();
            }
            if (usage.UseMessageCount)
            {
                values.UserMessageCount = await GetUserMessageCount();
                values.DialogMessageCount = await GetDialogMessageCount();
            }

            return values;
        }

        private async Task<string> GetServerAddress()
        {
            IDatabase database = _redisContext.GetDatabase();
            RedisValue value = await database.StringGetAsync(HistoryKeys.ServerAddress);
            return value;
        }

        private async Task<int> GetUserMessageCount()
        {
            IDatabase database = _redisContext.GetDatabase();
            RedisValue value = await database.StringGetAsync(HistoryKeys.UserMessageCount);
            value.TryParse(out int userMessageCount);
            return userMessageCount;
        }

        private async Task<int> GetDialogMessageCount()
        {
            IDatabase database = _redisContext.GetDatabase();
            RedisValue value = await database.StringGetAsync(HistoryKeys.DialogMessageCount);
            value.TryParse(out int dialogMessageCount);
            return dialogMessageCount;
        }
    }
}

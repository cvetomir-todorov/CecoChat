using System.Threading.Tasks;
using CecoChat.Redis;
using StackExchange.Redis;

namespace CecoChat.Data.Config.History
{
    internal interface IHistoryConfigurationRepository
    {
        Task<HistoryConfigurationValues> GetValues(HistoryConfigurationUsage usage);
    }

    internal sealed class HistoryConfigurationRepository : IHistoryConfigurationRepository
    {
        private readonly IRedisContext _redisContext;

        public HistoryConfigurationRepository(
            IRedisContext redisContext)
        {
            _redisContext = redisContext;
        }

        public async Task<HistoryConfigurationValues> GetValues(HistoryConfigurationUsage usage)
        {
            HistoryConfigurationValues values = new();

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

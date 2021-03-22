using System.Threading.Tasks;
using CecoChat.Redis;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration.History
{
    public interface IHistoryConfigurationRepository
    {
        Task<string> GetServerAddress();

        Task<int> GetUserMessageCount();

        Task<int> GetDialogMessageCount();
    }

    public sealed class HistoryConfigurationRepository : IHistoryConfigurationRepository
    {
        private readonly IRedisContext _redisContext;

        public HistoryConfigurationRepository(
            IRedisContext redisContext)
        {
            _redisContext = redisContext;
        }

        public async Task<string> GetServerAddress()
        {
            IDatabase database = _redisContext.GetDatabase();
            RedisValue value = await database.StringGetAsync(HistoryKeys.ServerAddress);
            return value;
        }

        public async Task<int> GetUserMessageCount()
        {
            IDatabase database = _redisContext.GetDatabase();
            RedisValue value = await database.StringGetAsync(HistoryKeys.UserMessageCount);
            value.TryParse(out int userMessageCount);
            return userMessageCount;
        }

        public async Task<int> GetDialogMessageCount()
        {
            IDatabase database = _redisContext.GetDatabase();
            RedisValue value = await database.StringGetAsync(HistoryKeys.DialogMessageCount);
            value.TryParse(out int dialogMessageCount);
            return dialogMessageCount;
        }
    }
}

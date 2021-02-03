using System.Threading.Tasks;
using CecoChat.Redis;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration.History
{
    public interface IHistoryConfigurationRepository
    {
        Task<string> GetServerAddress();
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
            const string key = HistoryKeys.ServerAddress;
            RedisValue value = await database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                throw new ConfigurationException(key);
            }

            return value;
        }
    }
}

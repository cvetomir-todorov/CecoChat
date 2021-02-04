using System.Threading.Tasks;
using CecoChat.Redis;
using StackExchange.Redis;

namespace CecoChat.Data.Configuration.History
{
    public interface IHistoryConfigurationRepository
    {
        Task<RedisValueResult<string>> GetServerAddress();

        Task<RedisValueResult<int>> GetUserMessageCount();

        Task<RedisValueResult<int>> GetDialogMessageCount();
    }

    public sealed class HistoryConfigurationRepository : IHistoryConfigurationRepository
    {
        private readonly IRedisContext _redisContext;

        public HistoryConfigurationRepository(
            IRedisContext redisContext)
        {
            _redisContext = redisContext;
        }

        public async Task<RedisValueResult<string>> GetServerAddress()
        {
            IDatabase database = _redisContext.GetDatabase();
            RedisValue value = await database.StringGetAsync(HistoryKeys.ServerAddress);

            if (value.IsNullOrEmpty)
            {
                return RedisValueResult<string>.Failure();
            }

            return RedisValueResult<string>.Success(value);
        }

        public async Task<RedisValueResult<int>> GetUserMessageCount()
        {
            IDatabase database = _redisContext.GetDatabase();
            RedisValue value = await database.StringGetAsync(HistoryKeys.UserMessageCount);

            if (value.IsNullOrEmpty ||
                !value.TryParse(out int userMessageCount))
            {
                return RedisValueResult<int>.Failure();
            }

            return RedisValueResult<int>.Success(userMessageCount);
        }

        public async Task<RedisValueResult<int>> GetDialogMessageCount()
        {
            IDatabase database = _redisContext.GetDatabase();
            RedisValue value = await database.StringGetAsync(HistoryKeys.DialogMessageCount);

            if (value.IsNullOrEmpty ||
                !value.TryParse(out int dialogMessageCount))
            {
                return RedisValueResult<int>.Failure();
            }

            return RedisValueResult<int>.Success(dialogMessageCount);
        }
    }
}

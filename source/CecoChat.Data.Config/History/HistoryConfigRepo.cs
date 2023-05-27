using CecoChat.Redis;
using StackExchange.Redis;

namespace CecoChat.Data.Config.History;

internal interface IHistoryConfigRepo
{
    Task<HistoryConfigValues> GetValues(HistoryConfigUsage usage);
}

internal sealed class HistoryConfigRepo : IHistoryConfigRepo
{
    private readonly IRedisContext _redisContext;

    public HistoryConfigRepo(
        IRedisContext redisContext)
    {
        _redisContext = redisContext;
    }

    public async Task<HistoryConfigValues> GetValues(HistoryConfigUsage usage)
    {
        HistoryConfigValues values = new();

        if (usage.UseMessageCount)
        {
            values.MessageCount = await GetMessageCount();
        }

        return values;
    }

    private async Task<int> GetMessageCount()
    {
        IDatabase database = _redisContext.GetDatabase();
        RedisValue value = await database.StringGetAsync(HistoryKeys.MessageCount);
        value.TryParse(out int messageCount);
        return messageCount;
    }
}

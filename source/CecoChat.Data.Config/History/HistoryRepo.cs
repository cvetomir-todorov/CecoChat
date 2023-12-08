﻿using CecoChat.Redis;
using StackExchange.Redis;

namespace CecoChat.Data.Config.History;

internal interface IHistoryRepo
{
    Task<HistoryValues> GetValues();
}

internal sealed class HistoryRepo : IHistoryRepo
{
    private readonly IRedisContext _redisContext;

    public HistoryRepo(
        IRedisContext redisContext)
    {
        _redisContext = redisContext;
    }

    public async Task<HistoryValues> GetValues()
    {
        HistoryValues values = new();

        values.MessageCount = await GetMessageCount();

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
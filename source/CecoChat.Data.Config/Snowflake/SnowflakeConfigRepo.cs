using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Config.Snowflake;

internal interface ISnowflakeConfigRepo
{
    Task<SnowflakeConfigValues> GetValues();
}

internal sealed class SnowflakeConfigRepo : ISnowflakeConfigRepo
{
    private readonly ILogger _logger;
    private readonly IRedisContext _redisContext;

    public SnowflakeConfigRepo(ILogger<SnowflakeConfigRepo> logger, IRedisContext redisContext)
    {
        _logger = logger;
        _redisContext = redisContext;
    }

    public async Task<SnowflakeConfigValues> GetValues()
    {
        SnowflakeConfigValues values = new();
        await GetGeneratorIds(values);
        return values;
    }

    private async Task GetGeneratorIds(SnowflakeConfigValues values)
    {
        IDatabase database = _redisContext.GetDatabase();
        HashEntry[] pairs = await database.HashGetAllAsync(SnowflakeKeys.GeneratorIds);

        foreach (HashEntry pair in pairs)
        {
            string? server = pair.Name;
            string? generatorIDsValue = pair.Value;

            if (server == null || generatorIDsValue == null)
            {
                _logger.LogError("Empty values are present in hash config {HashConfig}", SnowflakeKeys.GeneratorIds);
                continue;
            }

            List<short> generatorIds = new();
            values.GeneratorIds[server] = generatorIds;

            string[] generatorIdStrings = generatorIDsValue.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            foreach (string generatorIdString in generatorIdStrings)
            {
                if (short.TryParse(generatorIdString, out short generatorId))
                {
                    generatorIds.Add(generatorId);
                }
            }
        }
    }
}
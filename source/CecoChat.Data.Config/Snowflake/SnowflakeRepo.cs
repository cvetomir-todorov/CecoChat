using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Config.Snowflake;

internal interface ISnowflakeRepo
{
    Task<SnowflakeValues> GetValues();
}

internal sealed class SnowflakeRepo : ISnowflakeRepo
{
    private readonly ILogger _logger;
    private readonly ConfigDbContext _dbContext;

    public SnowflakeRepo(
        ILogger<SnowflakeRepo> logger,
        ConfigDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<SnowflakeValues> GetValues()
    {
        List<ElementEntity> elements = await _dbContext.Elements
            .Where(e => e.Name.StartsWith(ConfigKeys.Snowflake.Section))
            .AsNoTracking()
            .ToListAsync();

        SnowflakeValues values = new();

        ElementEntity? generatorIds = elements.FirstOrDefault(e => string.Equals(e.Name, ConfigKeys.Snowflake.GeneratorIds, StringComparison.InvariantCultureIgnoreCase));
        if (generatorIds != null)
        {
            ParseGeneratorIds(generatorIds, values);
        }

        return values;
    }

    private void ParseGeneratorIds(ElementEntity element, SnowflakeValues values)
    {
        string[] pairs = element.Value.Split(separator: ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string pair in pairs)
        {
            ParseGeneratorIdsValue(element, pair, values);
        }
    }

    private void ParseGeneratorIdsValue(ElementEntity element, string pair, SnowflakeValues values)
    {
        string[] parts = pair.Split(separator: '=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            _logger.LogError("Config {ConfigName} has an invalid pair '{Pair}'", element.Name, pair);
            return;
        }

        string server = parts[0];
        string generatorIdsValue = parts[1];

        List<short> generatorIds = new();
        if (!values.GeneratorIds.TryAdd(server, generatorIds))
        {
            _logger.LogError("Config {ConfigName} has duplicate server ID '{ServerId}'", element.Name, server);
            return;
        }

        string[] generatorIdStrings = generatorIdsValue.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string generatorIdString in generatorIdStrings)
        {
            if (short.TryParse(generatorIdString, out short generatorId))
            {
                generatorIds.Add(generatorId);
            }
            else
            {
                _logger.LogError("Config {ConfigName} has invalid generator IDs in '{Pair}'", element.Name, pair);
            }
        }
    }
}

using CecoChat.Config.Client;
using CecoChat.Config.Contracts;
using Microsoft.Extensions.Logging;

namespace CecoChat.Config.Sections.Snowflake;

internal sealed class SnowflakeRepo : IRepo<SnowflakeValues>
{
    private readonly ILogger _logger;
    private readonly IConfigClient _configClient;

    public SnowflakeRepo(
        ILogger<SnowflakeRepo> logger,
        IConfigClient configClient)
    {
        _logger = logger;
        _configClient = configClient;
    }

    public async Task<SnowflakeValues> Load(CancellationToken ct)
    {
        IReadOnlyCollection<ConfigElement> elements = await _configClient.GetConfigElements(ConfigKeys.Snowflake.Section, ct);
        SnowflakeValues values = new();

        ConfigElement? generatorIds = elements.FirstOrDefault(e => string.Equals(e.Name, ConfigKeys.Snowflake.GeneratorIds, StringComparison.InvariantCultureIgnoreCase));
        if (generatorIds != null)
        {
            ParseGeneratorIds(generatorIds, values);
        }

        return values;
    }

    private void ParseGeneratorIds(ConfigElement element, SnowflakeValues values)
    {
        string[] pairs = element.Value.Split(separator: ';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string pair in pairs)
        {
            ParseGeneratorIdsValue(element, pair, values);
        }
    }

    private void ParseGeneratorIdsValue(ConfigElement element, string pair, SnowflakeValues values)
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

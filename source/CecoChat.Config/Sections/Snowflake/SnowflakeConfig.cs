using Microsoft.Extensions.Logging;

namespace CecoChat.Config.Sections.Snowflake;

public interface ISnowflakeConfig
{
    Task<bool> Initialize(CancellationToken ct);

    IReadOnlyCollection<short> GetGeneratorIds(string server);
}

internal sealed class SnowflakeConfig : ISnowflakeConfig
{
    private readonly ILogger _logger;
    private readonly IConfigSection<SnowflakeValues> _section;

    public SnowflakeConfig(
        ILogger<SnowflakeConfig> logger,
        IConfigSection<SnowflakeValues> section)
    {
        _logger = logger;
        _section = section;
    }

    public IReadOnlyCollection<short> GetGeneratorIds(string server)
    {
        EnsureInitialized();

        if (!_section.Values!.GeneratorIds.TryGetValue(server, out List<short>? generatorIDs))
        {
            throw new InvalidOperationException($"No snowflake generator IDs configured for server {server}.");
        }

        return generatorIDs;
    }

    private void EnsureInitialized()
    {
        if (_section.Values == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the config.");
        }
    }

    public Task<bool> Initialize(CancellationToken ct)
    {
        return _section.Initialize(ConfigKeys.Snowflake.Section, PrintValues, ct);
    }

    private void PrintValues(SnowflakeValues values)
    {
        _logger.LogInformation("Total of {ServerCount} server(s) configured:", values.GeneratorIds.Count);
        foreach (KeyValuePair<string, List<short>> pair in values.GeneratorIds)
        {
            _logger.LogInformation("Server {Server} is assigned generator IDs: [{GeneratorIds}]", pair.Key, string.Join(separator: ", ", pair.Value));
        }
    }
}

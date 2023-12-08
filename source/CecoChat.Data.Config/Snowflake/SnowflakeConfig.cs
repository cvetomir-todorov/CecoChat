using System.Diagnostics.CodeAnalysis;
using CecoChat.Data.Config.Common;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Config.Snowflake;

public interface ISnowflakeConfig
{
    Task<bool> Initialize();

    IReadOnlyCollection<short> GetGeneratorIds(string server);
}

internal sealed class SnowflakeConfig : ISnowflakeConfig
{
    private readonly ILogger _logger;
    private readonly IConfigSection<SnowflakeValues> _section;
    private SnowflakeValues? _values;

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

        if (!_values.GeneratorIds.TryGetValue(server, out List<short>? generatorIDs))
        {
            throw new InvalidOperationException($"No snowflake generator IDs configured for server {server}.");
        }

        return generatorIDs;
    }

    public async Task<bool> Initialize()
    {
        InitializeResult<SnowflakeValues> result = await _section.Initialize(ConfigKeys.Snowflake.Section, PrintValues);
        _values = result.Values;

        return result.Success;
    }

    private void PrintValues(SnowflakeValues values)
    {
        _logger.LogInformation("Total of {ServerCount} server(s) configured:", values.GeneratorIds.Count);
        foreach (KeyValuePair<string, List<short>> pair in values.GeneratorIds)
        {
            _logger.LogInformation("Server {Server} is assigned generator IDs: [{GeneratorIds}]", pair.Key, string.Join(separator: ", ", pair.Value));
        }
    }

    [MemberNotNull(nameof(_values))]
    private void EnsureInitialized()
    {
        if (_values == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the config.");
        }
    }
}

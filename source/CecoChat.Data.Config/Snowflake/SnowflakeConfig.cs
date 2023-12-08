using Microsoft.Extensions.Logging;

namespace CecoChat.Data.Config.Snowflake;

internal sealed class SnowflakeConfig : ISnowflakeConfig
{
    private readonly ILogger _logger;
    private readonly ISnowflakeRepo _repo;
    private readonly IConfigUtility _configUtility;

    private SnowflakeValidator? _validator;
    private SnowflakeValues? _values;

    public SnowflakeConfig(
        ILogger<SnowflakeConfig> logger,
        ISnowflakeRepo repo,
        IConfigUtility configUtility)
    {
        _logger = logger;
        _repo = repo;
        _configUtility = configUtility;
    }

    public IReadOnlyCollection<short> GetGeneratorIds(string server)
    {
        EnsureInitialized();

        if (!_values!.GeneratorIds.TryGetValue(server, out List<short>? generatorIDs))
        {
            throw new InvalidOperationException($"No snowflake generator IDs configured for server {server}.");
        }

        return generatorIDs;
    }

    public async Task<bool> Initialize()
    {
        try
        {
            _validator = new SnowflakeValidator();
            bool areValid = await LoadValidateValues();

            return areValid;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Initializing snowflake config failed");
            return false;
        }
    }

    private async Task<bool> LoadValidateValues()
    {
        EnsureInitialized();

        SnowflakeValues values = await _repo.GetValues();
        _logger.LogInformation("Loading snowflake configuration succeeded");

        bool areValid = _configUtility.ValidateValues("snowflake", values, _validator!);
        if (areValid)
        {
            _values = values;
            PrintValues(values);
        }

        return areValid;
    }

    private void PrintValues(SnowflakeValues values)
    {
        _logger.LogInformation("Total of {ServerCount} server(s) configured:", values.GeneratorIds.Count);
        foreach (KeyValuePair<string, List<short>> pair in values.GeneratorIds)
        {
            _logger.LogInformation("Server {Server} is assigned generator IDs: [{GeneratorIds}]", pair.Key, string.Join(separator: ", ", pair.Value));
        }
    }

    private void EnsureInitialized()
    {
        if (_validator == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the config.");
        }
    }
}

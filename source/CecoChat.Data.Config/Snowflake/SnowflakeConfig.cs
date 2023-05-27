using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Config.Snowflake;

internal sealed class SnowflakeConfig : ISnowflakeConfig
{
    private readonly ILogger _logger;
    private readonly IRedisContext _redisContext;
    private readonly ISnowflakeConfigRepo _repo;
    private readonly IConfigUtility _configUtility;

    private SnowflakeConfigValidator? _validator;
    private SnowflakeConfigValues? _values;

    public SnowflakeConfig(
        ILogger<SnowflakeConfig> logger,
        IRedisContext redisContext,
        ISnowflakeConfigRepo repo,
        IConfigUtility configUtility)
    {
        _logger = logger;
        _redisContext = redisContext;
        _repo = repo;
        _configUtility = configUtility;
    }

    public void Dispose()
    {
        _redisContext.Dispose();
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

    public async Task Initialize()
    {
        try
        {
            _validator = new SnowflakeConfigValidator();
            await SubscribeForChanges();
            await LoadValidateValues();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Initializing snowflake config failed");
        }
    }

    private async Task SubscribeForChanges()
    {
        ISubscriber subscriber = _redisContext.GetSubscriber();

        ChannelMessageQueue generatorIdsChannel = await subscriber.SubscribeAsync($"notify:{SnowflakeKeys.GeneratorIds}");
        generatorIdsChannel.OnMessage(channelMessage => _configUtility.HandleChange(channelMessage, HandleGeneratorIDs));
        _logger.LogInformation("Subscribed for changes about {ServerGeneratorIDs} from channel {Channel}", SnowflakeKeys.GeneratorIds, generatorIdsChannel.Channel);
    }

    private Task HandleGeneratorIDs(ChannelMessage _)
    {
        return LoadValidateValues();
    }

    private async Task LoadValidateValues()
    {
        EnsureInitialized();

        SnowflakeConfigValues values = await _repo.GetValues();
        _logger.LogInformation("Loading snowflake configuration succeeded");

        bool areValid = _configUtility.ValidateValues("snowflake", values, _validator!);
        if (areValid)
        {
            _values = values;
            PrintValues(values);
        }
    }

    private void PrintValues(SnowflakeConfigValues values)
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

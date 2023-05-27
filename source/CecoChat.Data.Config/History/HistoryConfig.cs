using CecoChat.Redis;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CecoChat.Data.Config.History;

internal sealed class HistoryConfig : IHistoryConfig
{
    private readonly ILogger _logger;
    private readonly IRedisContext _redisContext;
    private readonly IHistoryConfigRepo _repo;
    private readonly IConfigUtility _configUtility;

    private HistoryConfigUsage? _usage;
    private HistoryConfigValidator? _validator;
    private HistoryConfigValues? _values;

    public HistoryConfig(
        ILogger<HistoryConfig> logger,
        IRedisContext redisContext,
        IHistoryConfigRepo repo,
        IConfigUtility configUtility)
    {
        _logger = logger;
        _redisContext = redisContext;
        _repo = repo;
        _configUtility = configUtility;
    }

    public int MessageCount
    {
        get
        {
            EnsureInitialized();
            return _values!.MessageCount;
        }
    }

    public async Task Initialize(HistoryConfigUsage usage)
    {
        try
        {
            _usage = usage;
            _validator = new HistoryConfigValidator(usage);
            await SubscribeForChanges(usage);
            await LoadValidateValues(usage);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Initializing history configuration failed");
        }
    }

    private async Task SubscribeForChanges(HistoryConfigUsage usage)
    {
        ISubscriber subscriber = _redisContext.GetSubscriber();

        if (usage.UseMessageCount)
        {
            ChannelMessageQueue messageCountChannel = await subscriber.SubscribeAsync($"notify:{HistoryKeys.MessageCount}");
            messageCountChannel.OnMessage(channelMessage => _configUtility.HandleChange(channelMessage, HandleMessageCount));
            _logger.LogInformation("Subscribed for changes about {MessageCount} from channel {Channel}", HistoryKeys.MessageCount, messageCountChannel.Channel);
        }
    }

    private async Task HandleMessageCount(ChannelMessage _)
    {
        EnsureInitialized();

        if (_usage!.UseMessageCount)
        {
            await LoadValidateValues(_usage);
        }
    }

    private async Task LoadValidateValues(HistoryConfigUsage usage)
    {
        EnsureInitialized();

        HistoryConfigValues values = await _repo.GetValues(usage);
        _logger.LogInformation("Loading history configuration succeeded");

        if (_configUtility.ValidateValues("history", values, _validator!))
        {
            _values = values;
            PrintValues(usage, values);
        }
    }

    private void PrintValues(HistoryConfigUsage usage, HistoryConfigValues values)
    {
        if (usage.UseMessageCount)
        {
            _logger.LogInformation("Chat message count set to {MessageCount}", values.MessageCount);
        }
    }

    private void EnsureInitialized()
    {
        if (_usage == null || _validator == null)
        {
            throw new InvalidOperationException($"Call '{nameof(Initialize)}' to initialize the config.");
        }
    }
}

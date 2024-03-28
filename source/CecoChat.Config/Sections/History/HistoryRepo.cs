using CecoChat.Config.Client;
using CecoChat.Config.Contracts;
using Microsoft.Extensions.Logging;

namespace CecoChat.Config.Sections.History;

internal sealed class HistoryRepo : IRepo<HistoryValues>
{
    private readonly ILogger _logger;
    private readonly IConfigClient _configClient;

    public HistoryRepo(
        ILogger<HistoryRepo> logger,
        IConfigClient configClient)
    {
        _logger = logger;
        _configClient = configClient;
    }

    public async Task<HistoryValues> Load(CancellationToken ct)
    {
        IReadOnlyCollection<ConfigElement> elements = await _configClient.GetConfigElements(ConfigKeys.History.Section, ct);
        HistoryValues values = new();

        ConfigElement? messageCount = elements.FirstOrDefault(e => string.Equals(e.Name, ConfigKeys.History.MessageCount, StringComparison.InvariantCultureIgnoreCase));
        if (messageCount != null)
        {
            values.MessageCount = ParseMessageCount(messageCount);
        }

        return values;
    }

    private int ParseMessageCount(ConfigElement element)
    {
        if (!int.TryParse(element.Value, out int messageCount))
        {
            _logger.LogError("Config {ConfigName} has invalid value '{ConfigValue}'", element.Name, element.Value);
        }

        return messageCount;
    }
}

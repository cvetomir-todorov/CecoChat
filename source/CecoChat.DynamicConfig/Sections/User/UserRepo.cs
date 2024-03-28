using CecoChat.Config.Client;
using CecoChat.Config.Contracts;
using Microsoft.Extensions.Logging;

namespace CecoChat.DynamicConfig.Sections.User;

internal sealed class UserRepo : IRepo<UserValues>
{
    private readonly ILogger _logger;
    private readonly IConfigClient _configClient;

    public UserRepo(
        ILogger<UserRepo> logger,
        IConfigClient configClient)
    {
        _logger = logger;
        _configClient = configClient;
    }

    public async Task<UserValues> Load(CancellationToken ct)
    {
        IReadOnlyCollection<ConfigElement> elements = await _configClient.GetConfigElements(ConfigKeys.User.Section, ct);
        UserValues values = new();

        ConfigElement? profileCount = elements.FirstOrDefault(e => string.Equals(e.Name, ConfigKeys.User.ProfileCount, StringComparison.InvariantCultureIgnoreCase));
        if (profileCount != null)
        {
            values.ProfileCount = ParseProfileCount(profileCount);
        }

        return values;
    }

    private int ParseProfileCount(ConfigElement element)
    {
        if (!int.TryParse(element.Value, out int profileCount))
        {
            _logger.LogError("Config {ConfigName} has invalid value '{ConfigValue}'", element.Name, element.Value);
        }

        return profileCount;
    }
}

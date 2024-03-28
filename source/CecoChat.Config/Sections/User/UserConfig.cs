using Microsoft.Extensions.Logging;

namespace CecoChat.Config.Sections.User;

public interface IUserConfig
{
    Task<bool> Initialize(CancellationToken ct);

    int ProfileCount { get; }
}

internal sealed class UserConfig : IUserConfig
{
    private readonly ILogger _logger;
    private readonly IConfigSection<UserValues> _section;

    public UserConfig(
        ILogger<UserConfig> logger,
        IConfigSection<UserValues> section)
    {
        _logger = logger;
        _section = section;
    }

    public int ProfileCount
    {
        get
        {
            EnsureInitialized();
            return _section.Values!.ProfileCount;
        }
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
        return _section.Initialize(ConfigKeys.User.Section, PrintValues, ct);
    }

    private void PrintValues(UserValues values)
    {
        _logger.LogInformation("User profile count set to {ProfileCount}", values.ProfileCount);
    }
}

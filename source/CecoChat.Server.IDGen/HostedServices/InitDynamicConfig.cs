using CecoChat.Data.Config.Snowflake;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.IDGen.HostedServices;

public sealed class InitDynamicConfig : IHostedService
{
    private readonly ILogger _logger;
    private readonly ConfigOptions _configOptions;
    private readonly ISnowflakeConfig _snowflakeConfig;

    public InitDynamicConfig(
        ILogger<InitDynamicConfig> logger,
        IOptions<ConfigOptions> configOptions,
        ISnowflakeConfig snowflakeConfig)
    {
        _logger = logger;
        _configOptions = configOptions.Value;
        _snowflakeConfig = snowflakeConfig;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Configured server ID is {ServerId}", _configOptions.ServerID);
        await _snowflakeConfig.Initialize();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
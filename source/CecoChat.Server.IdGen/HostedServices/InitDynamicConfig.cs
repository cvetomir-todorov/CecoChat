using CecoChat.Data.Config.Snowflake;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.IdGen.HostedServices;

public sealed class InitDynamicConfig : IHostedService
{
    private readonly ILogger _logger;
    private readonly ConfigOptions _configOptions;
    private readonly ISnowflakeConfig _snowflakeConfig;
    private readonly ConfigDbInitHealthCheck _configDbInitHealthCheck;

    public InitDynamicConfig(
        ILogger<InitDynamicConfig> logger,
        IOptions<ConfigOptions> configOptions,
        ISnowflakeConfig snowflakeConfig,
        ConfigDbInitHealthCheck configDbInitHealthCheck)
    {
        _logger = logger;
        _configOptions = configOptions.Value;
        _snowflakeConfig = snowflakeConfig;
        _configDbInitHealthCheck = configDbInitHealthCheck;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Configured server ID is {ServerId}", _configOptions.ServerID);
        await _snowflakeConfig.Initialize();

        _configDbInitHealthCheck.IsReady = true;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

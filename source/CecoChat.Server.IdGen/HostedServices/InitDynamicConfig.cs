using CecoChat.DynamicConfig.Snowflake;
using Microsoft.Extensions.Options;

namespace CecoChat.Server.IdGen.HostedServices;

public sealed class InitDynamicConfig : IHostedService
{
    private readonly ILogger _logger;
    private readonly ConfigOptions _configOptions;
    private readonly ISnowflakeConfig _snowflakeConfig;
    private readonly DynamicConfigInitHealthCheck _dynamicConfigInitHealthCheck;

    public InitDynamicConfig(
        ILogger<InitDynamicConfig> logger,
        IOptions<ConfigOptions> configOptions,
        ISnowflakeConfig snowflakeConfig,
        DynamicConfigInitHealthCheck dynamicConfigInitHealthCheck)
    {
        _logger = logger;
        _configOptions = configOptions.Value;
        _snowflakeConfig = snowflakeConfig;
        _dynamicConfigInitHealthCheck = dynamicConfigInitHealthCheck;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Configured server ID is {ServerId}", _configOptions.ServerId);
        _dynamicConfigInitHealthCheck.IsReady = await _snowflakeConfig.Initialize(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

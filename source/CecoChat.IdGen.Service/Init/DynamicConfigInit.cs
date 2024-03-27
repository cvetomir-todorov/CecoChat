using CecoChat.DynamicConfig.Sections.Snowflake;
using CecoChat.Server;
using Common.AspNet.Init;
using Microsoft.Extensions.Options;

namespace CecoChat.IdGen.Service.Init;

public sealed class DynamicConfigInit : InitStep
{
    private readonly ILogger _logger;
    private readonly ConfigOptions _configOptions;
    private readonly ISnowflakeConfig _snowflakeConfig;
    private readonly DynamicConfigInitHealthCheck _dynamicConfigInitHealthCheck;

    public DynamicConfigInit(
        ILogger<DynamicConfigInit> logger,
        IOptions<ConfigOptions> configOptions,
        ISnowflakeConfig snowflakeConfig,
        DynamicConfigInitHealthCheck dynamicConfigInitHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _logger = logger;
        _configOptions = configOptions.Value;
        _snowflakeConfig = snowflakeConfig;
        _dynamicConfigInitHealthCheck = dynamicConfigInitHealthCheck;
    }

    protected override async Task<bool> DoExecute(CancellationToken ct)
    {
        _logger.LogInformation("Configured server ID is {ServerId}", _configOptions.ServerId);
        _dynamicConfigInitHealthCheck.IsReady = await _snowflakeConfig.Initialize(ct);

        return _dynamicConfigInitHealthCheck.IsReady;
    }
}

using CecoChat.Config.Sections.Partitioning;
using CecoChat.Server;
using Common.AspNet.Init;
using Microsoft.Extensions.Options;

namespace CecoChat.Messaging.Service.Init;

public sealed class DynamicConfigInit : InitStep
{
    private readonly ILogger _logger;
    private readonly ConfigOptions _configOptions;
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly DynamicConfigInitHealthCheck _dynamicConfigInitHealthCheck;

    public DynamicConfigInit(
        ILogger<DynamicConfigInit> logger,
        IOptions<ConfigOptions> configOptions,
        IPartitioningConfig partitioningConfig,
        DynamicConfigInitHealthCheck dynamicConfigInitHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _logger = logger;
        _configOptions = configOptions.Value;
        _partitioningConfig = partitioningConfig;
        _dynamicConfigInitHealthCheck = dynamicConfigInitHealthCheck;
    }

    protected override async Task<bool> DoExecute(CancellationToken ct)
    {
        _logger.LogInformation("Configured server ID is {ServerId}", _configOptions.ServerId);
        _dynamicConfigInitHealthCheck.IsReady = await _partitioningConfig.Initialize(ct);

        return _dynamicConfigInitHealthCheck.IsReady;
    }
}

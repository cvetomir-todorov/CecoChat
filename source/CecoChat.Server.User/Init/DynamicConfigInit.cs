using CecoChat.AspNet.Init;
using CecoChat.DynamicConfig.Partitioning;

namespace CecoChat.Server.User.Init;

public class DynamicConfigInit : InitStep
{
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly DynamicConfigInitHealthCheck _dynamicConfigInitHealthCheck;

    public DynamicConfigInit(
        IPartitioningConfig partitioningConfig,
        DynamicConfigInitHealthCheck dynamicConfigInitHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _partitioningConfig = partitioningConfig;
        _dynamicConfigInitHealthCheck = dynamicConfigInitHealthCheck;
    }

    protected override async Task<bool> DoExecute(CancellationToken ct)
    {
        _dynamicConfigInitHealthCheck.IsReady = await _partitioningConfig.Initialize(ct);

        return _dynamicConfigInitHealthCheck.IsReady;
    }
}

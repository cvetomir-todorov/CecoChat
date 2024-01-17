using CecoChat.AspNet.Init;
using CecoChat.DynamicConfig.Partitioning;
using CecoChat.DynamicConfig.User;

namespace CecoChat.Server.Bff.Init;

public sealed class DynamicConfigInit : InitStep
{
    private readonly IPartitioningConfig _partitioningConfig;
    private readonly IUserConfig _userConfig;
    private readonly DynamicConfigInitHealthCheck _dynamicConfigInitHealthCheck;

    public DynamicConfigInit(
        IPartitioningConfig partitioningConfig,
        IUserConfig userConfig,
        DynamicConfigInitHealthCheck dynamicConfigInitHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _partitioningConfig = partitioningConfig;
        _userConfig = userConfig;
        _dynamicConfigInitHealthCheck = dynamicConfigInitHealthCheck;
    }

    protected override async Task<bool> DoExecute(CancellationToken ct)
    {
        _dynamicConfigInitHealthCheck.IsReady =
            await _partitioningConfig.Initialize(ct) &&
            await _userConfig.Initialize(ct);

        return _dynamicConfigInitHealthCheck.IsReady;
    }
}

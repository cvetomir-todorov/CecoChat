using CecoChat.DynamicConfig.Sections.Partitioning;
using CecoChat.DynamicConfig.Sections.User;
using CecoChat.Server;
using Common.AspNet.Init;

namespace CecoChat.User.Service.Init;

public class DynamicConfigInit : InitStep
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

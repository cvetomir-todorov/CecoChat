using CecoChat.DynamicConfig.Sections.History;
using CecoChat.Server;
using Common.AspNet.Init;

namespace CecoChat.Chats.Service.Init;

public sealed class DynamicConfigInit : InitStep
{
    private readonly IHistoryConfig _historyConfig;
    private readonly DynamicConfigInitHealthCheck _dynamicConfigInitHealthCheck;

    public DynamicConfigInit(
        IHistoryConfig historyConfig,
        DynamicConfigInitHealthCheck dynamicConfigInitHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _historyConfig = historyConfig;
        _dynamicConfigInitHealthCheck = dynamicConfigInitHealthCheck;
    }

    protected override async Task<bool> DoExecute(CancellationToken ct)
    {
        _dynamicConfigInitHealthCheck.IsReady = await _historyConfig.Initialize(ct);

        return _dynamicConfigInitHealthCheck.IsReady;
    }
}

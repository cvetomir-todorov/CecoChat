using CecoChat.DynamicConfig.History;

namespace CecoChat.Server.Chats.HostedServices;

public sealed class InitDynamicConfig : IHostedService
{
    private readonly IHistoryConfig _historyConfig;
    private readonly DynamicConfigInitHealthCheck _dynamicConfigInitHealthCheck;

    public InitDynamicConfig(
        IHistoryConfig historyConfig,
        DynamicConfigInitHealthCheck dynamicConfigInitHealthCheck)
    {
        _historyConfig = historyConfig;
        _dynamicConfigInitHealthCheck = dynamicConfigInitHealthCheck;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _dynamicConfigInitHealthCheck.IsReady = await _historyConfig.Initialize(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

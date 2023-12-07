using CecoChat.Data.Config.History;

namespace CecoChat.Server.Chats.HostedServices;

public sealed class InitDynamicConfig : IHostedService
{
    private readonly IHistoryConfig _historyConfig;
    private readonly ConfigDbInitHealthCheck _configDbInitHealthCheck;

    public InitDynamicConfig(
        IHistoryConfig historyConfig,
        ConfigDbInitHealthCheck configDbInitHealthCheck)
    {
        _historyConfig = historyConfig;
        _configDbInitHealthCheck = configDbInitHealthCheck;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _configDbInitHealthCheck.IsReady = await _historyConfig.Initialize();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

using CecoChat.Data.User.Profiles;

namespace CecoChat.Server.User.HostedServices;

public class AsyncProfileCaching : IHostedService
{
    private readonly IProfileCache _profileCache;

    public AsyncProfileCaching(IProfileCache profileCache)
    {
        _profileCache = profileCache;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _profileCache.StartProcessing(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

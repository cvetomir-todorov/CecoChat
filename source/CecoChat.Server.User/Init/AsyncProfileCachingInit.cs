using CecoChat.AspNet.Init;
using CecoChat.Data.User.Entities.Profiles;

namespace CecoChat.Server.User.Init;

public class AsyncProfileCachingInit : InitStep
{
    private readonly IProfileCache _profileCache;

    public AsyncProfileCachingInit(
        IProfileCache profileCache,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _profileCache = profileCache;
    }

    protected override Task<bool> DoExecute(CancellationToken ct)
    {
        _profileCache.StartProcessing(ct);
        return Task.FromResult(true);
    }
}

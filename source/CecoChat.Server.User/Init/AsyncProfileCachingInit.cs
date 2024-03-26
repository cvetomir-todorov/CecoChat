using CecoChat.Data.User.Entities.Profiles;
using Common.AspNet.Init;

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

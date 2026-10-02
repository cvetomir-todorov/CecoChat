using CecoChat.User.Data.Entities.Profiles;
using Common.AspNet.Init;

namespace CecoChat.User.Service.Init;

public class AsyncProfileCachingInit : InitStep
{
    private readonly IProfileCache _profileCache;

    public AsyncProfileCachingInit(IProfileCache profileCache)
    {
        _profileCache = profileCache;
    }

    public override Task<bool> Execute(CancellationToken ct)
    {
        _profileCache.StartProcessing(ct);
        return Task.FromResult(true);
    }
}

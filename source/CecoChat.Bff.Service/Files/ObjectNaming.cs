using Common;

namespace CecoChat.Bff.Service.Files;

public interface IObjectNaming
{
    string GetCurrentBucketName();

    string CreateObjectName(long userId, string extensionWithDot);
}

public class ObjectNaming : IObjectNaming
{
    private readonly IClock _clock;

    public ObjectNaming(IClock clock)
    {
        _clock = clock;
    }

    public string GetCurrentBucketName()
    {
        DateTime now = _clock.GetNowUtc();
        string bucketName = $"files-{now.Year}-{now.Month:00}-{now.Day:00}";
        return bucketName;
    }

    public string CreateObjectName(long userId, string extensionWithDot)
    {
        string objectName = $"{userId}/{Guid.NewGuid()}{extensionWithDot}";
        return objectName;
    }
}

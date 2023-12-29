namespace CecoChat.Server.Bff.Files;

public interface IFileStorage
{
    string GetCurrentBucketName();

    string CreateObjectName(long userId, string extensionWithDot);
}

public class FileStorage : IFileStorage
{
    private readonly IClock _clock;

    public FileStorage(IClock clock)
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

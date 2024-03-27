namespace CecoChat.User.Data.Entities.Files;

public interface IFileCommandRepo
{
    Task<AssociateFileResult> AssociateFile(long userId, string bucket, string path, long allowedUserId);

    Task<AddFileAccessResult> AddFileAccess(long userId, string bucket, string path, DateTime version, long allowedUserId);
}

public readonly struct AssociateFileResult
{
    public bool Success { get; init; }
    public DateTime Version { get; init; }
    public bool Duplicate { get; init; }
}

public readonly struct AddFileAccessResult
{
    public bool Success { get; init; }
    public DateTime NewVersion { get; init; }
    public bool ConcurrentlyUpdated { get; init; }
}

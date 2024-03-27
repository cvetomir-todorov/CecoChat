using CecoChat.User.Contracts;

namespace CecoChat.User.Client;

public interface IFileClient
{
    Task<IReadOnlyCollection<FileRef>> GetUserFiles(long userId, DateTime newerThan, string accessToken, CancellationToken ct);

    Task<AssociateFileResult> AssociateFile(long userId, string bucket, string path, long allowedUserId, string accessToken, CancellationToken ct);

    Task<AddFileAccessResult> AddFileAccess(long userId, string bucket, string path, DateTime version, long allowedUserId, string accessToken, CancellationToken ct);

    Task<bool> HasUserFileAccess(long userId, string bucket, string path, string accessToken, CancellationToken ct);
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

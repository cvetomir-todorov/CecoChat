using CecoChat.Contracts.User;

namespace CecoChat.Client.User;

public interface IFileClient
{
    Task<IReadOnlyCollection<FileRef>> GetUserFiles(long userId, DateTime newerThan, string accessToken, CancellationToken ct);

    Task<AssociateFileResult> AssociateFile(long userId, string bucket, string path, string accessToken, CancellationToken ct);
}

public readonly struct AssociateFileResult
{
    public bool Success { get; init; }
    public DateTime Version { get; init; }
    public bool Duplicate { get; init; }
}

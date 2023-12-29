namespace CecoChat.Client.User;

public interface IFileClient
{
    Task<AddFileResult> AddFile(long userId, string bucket, string path, string accessToken, CancellationToken ct);
}

public readonly struct AddFileResult
{
    public bool Success { get; init; }
    public DateTime Version { get; init; }
    public bool DuplicateFile { get; init; }
}

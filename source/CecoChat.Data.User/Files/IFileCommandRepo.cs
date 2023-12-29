namespace CecoChat.Data.User.Files;

public interface IFileCommandRepo
{
    Task<AddFileResult> AddFile(long userId, string bucket, string path);
}

public readonly struct AddFileResult
{
    public bool Success { get; init; }
    public DateTime Version { get; init; }
    public bool DuplicateFile { get; init; }
}

namespace CecoChat.Data.User.Files;

public interface IFileCommandRepo
{
    Task<AssociateFileResult> AssociateFile(long userId, string bucket, string path);
}

public readonly struct AssociateFileResult
{
    public bool Success { get; init; }
    public DateTime Version { get; init; }
    public bool Duplicate { get; init; }
}

using CecoChat.User.Contracts;

namespace CecoChat.User.Data.Entities.Files;

public interface IFileQueryRepo
{
    Task<IReadOnlyCollection<FileRef>> GetUserFiles(long userId, DateTime newerThan);

    Task<bool> HasUserFileAccess(long userId, string bucket, string path);
}

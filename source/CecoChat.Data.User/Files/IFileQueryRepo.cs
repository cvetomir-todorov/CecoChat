using CecoChat.Contracts.User;

namespace CecoChat.Data.User.Files;

public interface IFileQueryRepo
{
    Task<IReadOnlyCollection<FileRef>> GetUserFiles(long userId, DateTime newerThan);
}

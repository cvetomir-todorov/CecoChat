using CecoChat.Contracts.User;

namespace CecoChat.Data.User.Entities.Files;

public interface IFileQueryRepo
{
    Task<IReadOnlyCollection<FileRef>> GetUserFiles(long userId, DateTime newerThan);
}

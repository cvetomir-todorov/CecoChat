using CecoChat.Contracts.User;

namespace CecoChat.Data.User.Files;

public interface IFileQueryRepo
{
    // TODO: add date time filter
    Task<IReadOnlyCollection<FileRef>> GetUserFiles(long userId);
}

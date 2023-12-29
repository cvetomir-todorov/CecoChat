using File = CecoChat.Contracts.User.File;

namespace CecoChat.Data.User.Files;

public interface IFileQueryRepo
{
    // TODO: add date time filter
    Task<IReadOnlyCollection<File>> GetUserFiles(long userId);
}

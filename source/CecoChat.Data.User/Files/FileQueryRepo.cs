using CecoChat.Contracts.User;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CecoChat.Data.User.Files;

internal sealed class FileQueryRepo : IFileQueryRepo
{
    private readonly ILogger _logger;
    private readonly UserDbContext _dbContext;

    public FileQueryRepo(
        ILogger<FileQueryRepo> logger,
        UserDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<FileRef>> GetUserFiles(long userId)
    {
        List<FileRef> files = await _dbContext.Files
            .Where(entity => entity.UserId == userId)
            .Select(entity => MapFile(entity))
            .AsNoTracking()
            .ToListAsync();

        _logger.LogTrace("Fetched {FileCount} files for user {UserId}", files.Count, userId);
        return files;
    }

    private static FileRef MapFile(FileEntity entity)
    {
        return new FileRef
        {
            Bucket = entity.Bucket,
            Path = entity.Path,
            Version = entity.Version.ToTimestamp()
        };
    }
}

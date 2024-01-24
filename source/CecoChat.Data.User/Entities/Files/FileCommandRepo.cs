using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CecoChat.Data.User.Entities.Files;

internal class FileCommandRepo : IFileCommandRepo
{
    private readonly ILogger _logger;
    private readonly UserDbContext _dbContext;

    public FileCommandRepo(
        ILogger<FileCommandRepo> logger,
        UserDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<AssociateFileResult> AssociateFile(long userId, string bucket, string path, long allowedUserId)
    {
        FileEntity entity = new()
        {
            Bucket = bucket,
            Path = path,
            UserId = userId,
            Version = DateTime.UtcNow
        };

        if (allowedUserId > 0)
        {
            entity.AllowedUsers = new[] { allowedUserId };
        }

        _dbContext.Files.Add(entity);

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("Associated a file in bucket {Bucket} with path {Path} and user {UserId}", bucket, path, userId);
            return new AssociateFileResult
            {
                Success = true,
                Version = entity.Version
            };
        }
        catch (DbUpdateException dbUpdateException) when (dbUpdateException.InnerException is PostgresException postgresException)
        {
            // https://www.postgresql.org/docs/current/errcodes-appendix.html
            if (postgresException.SqlState == "23505")
            {
                if (postgresException.MessageText.Contains("Files_pkey"))
                {
                    _logger.LogTrace("Duplicate association between the file in bucket {Bucket} with path {Path} and user {UserId}", bucket, path, userId);
                    return new AssociateFileResult
                    {
                        Duplicate = true
                    };
                }
            }

            throw;
        }
    }
}

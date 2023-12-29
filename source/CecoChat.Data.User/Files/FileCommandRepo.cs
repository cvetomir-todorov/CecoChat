using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CecoChat.Data.User.Files;

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

    public async Task<AddFileResult> AddFile(long userId, string bucket, string path)
    {
        FileEntity entity = new()
        {
            Bucket = bucket,
            Path = path,
            UserId = userId,
            Version = DateTime.UtcNow
        };

        _dbContext.Files.Add(entity);

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogTrace("Added a new file to bucket {Bucket} with path {Path} for user {UserId}", bucket, path, userId);
            return new AddFileResult
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
                    return new AddFileResult
                    {
                        DuplicateFile = true
                    };
                }
            }

            throw;
        }
    }
}

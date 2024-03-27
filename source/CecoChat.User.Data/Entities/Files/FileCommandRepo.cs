using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace CecoChat.User.Data.Entities.Files;

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
                if (postgresException.MessageText.Contains("files_pkey"))
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

    public async Task<AddFileAccessResult> AddFileAccess(long userId, string bucket, string path, DateTime version, long allowedUserId)
    {
        // we need to append the allowed user ID to the array of existing allowed user IDs
        // since the array is not a set, appending always adds the user ID
        // therefore the condition includes:
        // - PK check: both bucket and path
        // - checking if the version matches to avoid concurrency issues
        // - checking if the existing allowed user IDs doesn't already contains our user ID
        // unfortunately, if the affected row count is 0, we don't know what is the issue:
        // - either the version doesn't match
        // - or the allowed user ID already exists in the array
        // therefore we check if the user ID already exists beforehand
        // there is a possibility that the user ID will be added in between checking and updating
        // which will be a false error, but likelihood is small

        long[] currentlyAllowedUserIds = await _dbContext.Files
            .Where(f => f.Bucket == bucket &&
                        f.Path == path &&
                        f.Version == version)
            .Select(f => f.AllowedUsers)
            .AsNoTracking()
            .FirstOrDefaultAsync() ?? Array.Empty<long>();

        if (currentlyAllowedUserIds.Contains(allowedUserId))
        {
            _logger.LogTrace(
                "File-access already exists for user ID {AllowedUserId} to file in bucket {Bucket} with path {Path} owned by user {UserId}",
                allowedUserId, bucket, path, userId);
            return new AddFileAccessResult
            {
                Success = true,
                NewVersion = version
            };
        }

        DateTime newVersion = DateTime.UtcNow;
        await using NpgsqlCommand command = CreateCommand(bucket, path, version, allowedUserId, newVersion);

        int affectedRows = await ExecuteCommand(command);
        if (affectedRows == 1)
        {
            _logger.LogTrace(
                "Added file-access for user ID {AllowedUserId} to file in bucket {Bucket} with path {Path} owned by user {UserId}",
                allowedUserId, bucket, path, userId);
            return new AddFileAccessResult
            {
                Success = true,
                NewVersion = newVersion
            };
        }
        if (affectedRows == 0)
        {
            _logger.LogTrace(
                "Failed to add file-access for user ID {AllowedUserId} to file in bucket {Bucket} with path {Path} owned by user {UserId} because the file has been concurrently updated",
                allowedUserId, bucket, path, userId);
            return new AddFileAccessResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new InvalidOperationException($"Failed to add file-access for user {allowedUserId} to file in bucket {bucket} with path {path} since query affected {affectedRows} rows instead of 0 or 1.");
    }

    private const string AddFileAccessCommand =
        "UPDATE public.files SET " +
        "allowed_users = array_append(allowed_users, :user_id)," +
        "version = :new_version " +
        "WHERE " +
        "bucket = :bucket AND " +
        "path = :path AND " +
        "version = :version AND NOT (:user_id = ANY(allowed_users))";

    private NpgsqlCommand CreateCommand(string bucket, string path, DateTime version, long allowedUserId, DateTime newVersion)
    {
        DbConnection connection = _dbContext.Database.GetDbConnection();
        NpgsqlCommand command = (NpgsqlCommand)connection.CreateCommand();

        // append the user ID to the list of allowed user IDs
        // where the bucket and path matches and
        // where the version matches the expected one
        // where the allowed user IDs do not already contain our user ID
        command.CommandText = AddFileAccessCommand;

        command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, allowedUserId);
        command.Parameters.AddWithValue("bucket", NpgsqlDbType.Text, bucket);
        command.Parameters.AddWithValue("path", NpgsqlDbType.Text, path);
        command.Parameters.AddWithValue("version", NpgsqlDbType.TimestampTz, version);
        command.Parameters.AddWithValue("new_version", NpgsqlDbType.TimestampTz, newVersion);

        return command;
    }

    private static async Task<int> ExecuteCommand(NpgsqlCommand command)
    {
        try
        {
            await command.Connection!.OpenAsync();
            int affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows;
        }
        finally
        {
            await command.Connection!.CloseAsync();
        }
    }
}

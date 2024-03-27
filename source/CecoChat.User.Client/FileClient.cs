using CecoChat.User.Contracts;
using Common;
using Common.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.User.Client;

internal sealed class FileClient : IFileClient
{
    private readonly ILogger _logger;
    private readonly UserClientOptions _options;
    private readonly FileCommand.FileCommandClient _fileCommandClient;
    private readonly FileQuery.FileQueryClient _fileQueryClient;
    private readonly IClock _clock;

    public FileClient(
        ILogger<FileClient> logger,
        IOptions<UserClientOptions> options,
        FileCommand.FileCommandClient fileCommandClient,
        FileQuery.FileQueryClient fileQueryClient,
        IClock clock)
    {
        _logger = logger;
        _options = options.Value;
        _fileCommandClient = fileCommandClient;
        _fileQueryClient = fileQueryClient;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<FileRef>> GetUserFiles(long userId, DateTime newerThan, string accessToken, CancellationToken ct)
    {
        GetUserFilesRequest request = new();
        request.NewerThan = newerThan.ToTimestamp();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        GetUserFilesResponse response = await _fileQueryClient.GetUserFilesAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received {FileCount} files for user {UserId} which are newer than {NewerThan}", response.Files.Count, userId, newerThan);
        return response.Files;
    }

    public async Task<AssociateFileResult> AssociateFile(long userId, string bucket, string path, long allowedUserId, string accessToken, CancellationToken ct)
    {
        AssociateFileRequest request = new();
        request.Bucket = bucket;
        request.Path = path;
        request.AllowedUserId = allowedUserId;

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        AssociateFileResponse response = await _fileCommandClient.AssociateFileAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            _logger.LogTrace("Received a successful association between file in bucket {Bucket} with path {Path} and user {UserId}", bucket, path, userId);
            return new AssociateFileResult
            {
                Success = true,
                Version = response.Version.ToDateTime()
            };
        }
        if (response.Duplicate)
        {
            _logger.LogTrace("Received a duplicate association between file in bucket {Bucket} with path {Path} and user {UserId}", bucket, path, userId);
            return new AssociateFileResult
            {
                Duplicate = true
            };
        }

        throw new ProcessingFailureException(typeof(AssociateFileResponse));
    }

    public async Task<AddFileAccessResult> AddFileAccess(long userId, string bucket, string path, DateTime version, long allowedUserId, string accessToken, CancellationToken ct)
    {
        AddFileAccessRequest request = new();
        request.Bucket = bucket;
        request.Path = path;
        request.AllowedUserId = allowedUserId;
        request.Version = version.ToTimestamp();

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        AddFileAccessResponse response = await _fileCommandClient.AddFileAccessAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            _logger.LogTrace(
                "Received a successful file-access addition for user ID {AllowedUserId} to file in bucket {Bucket} with path {Path} owned by user {UserId}",
                allowedUserId, bucket, path, userId);
            return new AddFileAccessResult
            {
                Success = true,
                NewVersion = response.NewVersion.ToDateTime()
            };
        }
        if (response.ConcurrentlyUpdated)
        {
            _logger.LogTrace(
                "Received a failed file-access addition for user ID {AllowedUserId} to file in bucket {Bucket} with path {Path} owned by user {UserId} because the file has been concurrently updated",
                allowedUserId, bucket, path, userId);
            return new AddFileAccessResult
            {
                ConcurrentlyUpdated = true
            };
        }

        throw new ProcessingFailureException(typeof(AddFileAccessResponse));
    }

    public async Task<bool> HasUserFileAccess(long userId, string bucket, string path, string accessToken, CancellationToken ct)
    {
        HasUserFileAccessRequest request = new();
        request.Bucket = bucket;
        request.Path = path;

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        HasUserFileAccessResponse response = await _fileQueryClient.HasUserFileAccessAsync(request, headers, deadline, ct);

        _logger.LogTrace("Received user {UserId} {Access} to file in bucket {Bucket} with path {Path}", userId, response.HasAccess ? "has access" : "has no access", bucket, path);
        return response.HasAccess;
    }
}

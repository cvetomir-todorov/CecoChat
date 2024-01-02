using CecoChat.Contracts.User;
using CecoChat.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.User;

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

    public async Task<AddFileResult> AddFile(long userId, string bucket, string path, string accessToken, CancellationToken ct)
    {
        AddFileRequest request = new();
        request.Bucket = bucket;
        request.Path = path;

        Metadata headers = new();
        headers.AddAuthorization(accessToken);
        DateTime deadline = _clock.GetNowUtc().Add(_options.CallTimeout);
        AddFileResponse response = await _fileCommandClient.AddFileAsync(request, headers, deadline, ct);

        if (response.Success)
        {
            _logger.LogTrace("Received a successful addition of file to bucket {Bucket} with path {Path} for user {UserId}", bucket, path, userId);
            return new AddFileResult
            {
                Success = true,
                Version = response.Version.ToDateTime()
            };
        }
        if (response.DuplicateFile)
        {
            _logger.LogTrace("Received a failed addition of duplicate file to bucket {Bucket} with path {Path} for user {UserId}", bucket, path, userId);
            return new AddFileResult
            {
                DuplicateFile = true
            };
        }

        throw new ProcessingFailureException(typeof(AddFileResponse));
    }
}

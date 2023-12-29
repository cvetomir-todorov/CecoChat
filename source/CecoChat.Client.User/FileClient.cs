using CecoChat.Contracts.User;
using CecoChat.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CecoChat.Client.User;

internal sealed class FileClient : IFileClient
{
    private readonly ILogger _logger;
    private readonly UserClientOptions _options;
    private readonly FileCommand.FileCommandClient _fileCommandClient;
    private readonly IClock _clock;

    public FileClient(
        ILogger<FileClient> logger,
        IOptions<UserClientOptions> options,
        FileCommand.FileCommandClient fileCommandClient,
        IClock clock)
    {
        _logger = logger;
        _options = options.Value;
        _fileCommandClient = fileCommandClient;
        _clock = clock;
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

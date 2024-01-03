using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Minio.DataModel.Tags;
using Minio.Exceptions;

namespace CecoChat.Minio;

public interface IMinioContext
{
    Task<bool> EnsureBucketExists(string bucketName, CancellationToken ct);

    Task<string> UploadFile(string bucketName, string objectName, IDictionary<string, string>? tags, Stream dataStream, long dataLength, CancellationToken ct);

    Task<DownloadFileResult> DownloadFile(string bucketName, string objectName, CancellationToken ct);
}

public readonly struct DownloadFileResult
{
    public bool IsFound { get; init; }
    public Stream Stream { get; init; }
    public string ContentType { get; init; }
}

internal class MinioContext : IMinioContext
{
    private readonly ILogger _logger;
    private readonly IMinioClient _minio;

    public MinioContext(
        ILogger<MinioContext> logger,
        IMinioClient minio)
    {
        _logger = logger;
        _minio = minio;
    }

    public async Task<bool> EnsureBucketExists(string bucketName, CancellationToken ct)
    {
        BucketExistsArgs bucketExistsArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        bool exists = await _minio.BucketExistsAsync(bucketExistsArgs, ct);
        if (exists)
        {
            _logger.LogInformation("Bucket {BucketName} already exists, skip creating", bucketName);
            return true;
        }

        MakeBucketArgs makeBucketArgs = new MakeBucketArgs()
            .WithBucket(bucketName);

        try
        {
            await _minio.MakeBucketAsync(makeBucketArgs, ct);
            _logger.LogInformation("Bucket {BucketName} created successfully", bucketName);
            return true;
        }
        catch (MinioException minioException)
        {
            _logger.LogError(minioException, "Failed to create bucket {BucketName}", bucketName);
            return false;
        }
    }

    public async Task<string> UploadFile(string bucketName, string objectName, IDictionary<string, string>? tags, Stream dataStream, long dataLength, CancellationToken ct)
    {
        PutObjectArgs putObjectArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithStreamData(dataStream)
            .WithObjectSize(dataLength);

        if (tags != null && tags.Count > 0)
        {
            putObjectArgs.WithTagging(new Tagging(tags, isObjects: true));
        }

        PutObjectResponse response = await _minio.PutObjectAsync(putObjectArgs, ct);
        return response.ObjectName;
    }

    public async Task<DownloadFileResult> DownloadFile(string bucketName, string objectName, CancellationToken ct)
    {
        // the MinIO API doesn't support returning the stream to a file
        // as a temporary workaround we buffer the whole file into memory
        // files uploaded have a limited size and are inherently immutable
        // clients should be implemented to cache files
        // rate limiting would also control the number of file download requests
        // the GitHub issue that was raised is here: https://github.com/minio/minio-dotnet/issues/973
        // in the addition of the old one which had been incorrectly closed: https://github.com/minio/minio-dotnet/issues/225

        const int initialBufferSize = 256 * 1024; // 256 KB
        MemoryStream memoryStream = new(capacity: initialBufferSize);

        GetObjectArgs getObjectArgs = new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithCallbackStream(async (minioStream, token) =>
            {
                const int bufferSize = 64 * 1024; // 64 KB
                await minioStream.CopyToAsync(memoryStream, bufferSize, token);
            });

        // TODO: catch exceptions when bucket/object doesn't exist

        ObjectStat stat = await _minio.GetObjectAsync(getObjectArgs, ct);
        // reset the stream so it can be read from the beginning by the client
        memoryStream.Seek(offset: 0, SeekOrigin.Begin);

        return new DownloadFileResult
        {
            IsFound = true,
            Stream = memoryStream,
            ContentType = stat.ContentType
        };
    }
}

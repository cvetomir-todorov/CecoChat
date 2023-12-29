using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Minio.DataModel.Tags;
using Minio.Exceptions;

namespace CecoChat.Minio;

public interface IMinioContext
{
    Task<bool> EnsureBucketExists(string bucketName, CancellationToken ct);

    Task<string> UploadFile(string bucketName, string objectName, IDictionary<string, string>? tags, Stream dataStream, long dataLength, CancellationToken ct);
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
}

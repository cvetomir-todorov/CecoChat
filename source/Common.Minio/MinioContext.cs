using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Response;
using Minio.DataModel.Tags;
using Minio.Exceptions;

namespace Common.Minio;

public interface IMinioContext
{
    Task<bool> EnsureBucketExists(string bucketName, CancellationToken ct);

    Task<ObjectTagsResult> GetObjectTags(string bucketName, string objectName, CancellationToken ct);

    Task<string> UploadObject(string bucketName, string objectName, string contentType, IDictionary<string, string>? tags, Stream dataStream, long dataLength, CancellationToken ct);

    Task<ObjectMetadataResult> GetObjectMetadata(string bucketName, string objectName, CancellationToken ct);

    Task<DownloadFileResult> WriteObjectToStream(string bucketName, string objectName, Stream targetStream, CancellationToken ct);
}

public readonly struct ObjectTagsResult
{
    public bool IsFound { get; init; }
    public IReadOnlyDictionary<string, string> Tags { get; init; }
}

public readonly struct ObjectMetadataResult
{
    public bool IsFound { get; init; }
    public string ContentType { get; init; }
    public long Size { get; init; }
}

public readonly struct DownloadFileResult
{
    public bool IsFound { get; init; }
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

    public async Task<ObjectTagsResult> GetObjectTags(string bucketName, string objectName, CancellationToken ct)
    {
        try
        {
            GetObjectTagsArgs getObjectTagsArgs = new GetObjectTagsArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            Tagging tagging = await _minio.GetObjectTagsAsync(getObjectTagsArgs, ct);

            return new ObjectTagsResult
            {
                IsFound = true,
                Tags = new ReadOnlyDictionary<string, string>(tagging.Tags)
            };
        }
        catch (BucketNotFoundException bucketNotFoundException)
        {
            _logger.LogError(bucketNotFoundException, "Error when getting tags from bucket {Bucket} for the object {Object}", bucketName, objectName);
            return new ObjectTagsResult
            {
                IsFound = false
            };
        }
        catch (ObjectNotFoundException objectNotFoundException)
        {
            _logger.LogError(objectNotFoundException, "Error when getting tags from bucket {Bucket} for the object {Object}", bucketName, objectName);
            return new ObjectTagsResult
            {
                IsFound = false
            };
        }
    }

    public async Task<string> UploadObject(string bucketName, string objectName, string contentType, IDictionary<string, string>? tags, Stream dataStream, long dataLength, CancellationToken ct)
    {
        PutObjectArgs putObjectArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithContentType(contentType)
            .WithStreamData(dataStream)
            .WithObjectSize(dataLength);

        if (tags != null && tags.Count > 0)
        {
            putObjectArgs.WithTagging(new Tagging(tags, isObjects: true));
        }

        PutObjectResponse response = await _minio.PutObjectAsync(putObjectArgs, ct);
        return response.ObjectName;
    }

    public async Task<ObjectMetadataResult> GetObjectMetadata(string bucketName, string objectName, CancellationToken ct)
    {
        try
        {
            StatObjectArgs statObjectArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName);
            ObjectStat stat = await _minio.StatObjectAsync(statObjectArgs, ct);

            return new ObjectMetadataResult
            {
                IsFound = true,
                ContentType = stat.ContentType,
                Size = stat.Size
            };
        }
        catch (BucketNotFoundException bucketNotFoundException)
        {
            _logger.LogError(bucketNotFoundException, "Error when getting metadata for bucket {Bucket} and object {Object}", bucketName, objectName);
            return new ObjectMetadataResult
            {
                IsFound = false
            };
        }
        catch (ObjectNotFoundException objectNotFoundException)
        {
            _logger.LogError(objectNotFoundException, "Error when getting metadata for bucket {Bucket} and object {Object}", bucketName, objectName);
            return new ObjectMetadataResult
            {
                IsFound = false
            };
        }
    }

    public async Task<DownloadFileResult> WriteObjectToStream(string bucketName, string objectName, Stream targetStream, CancellationToken ct)
    {
        try
        {
            const int bufferSize = 128 * 1024; // 128 KB

            GetObjectArgs getObjectArgs = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream((minioStream, minioCt) => minioStream.CopyToAsync(targetStream, bufferSize, minioCt));
            await _minio.GetObjectAsync(getObjectArgs, ct);

            return new DownloadFileResult
            {
                IsFound = true,
            };
        }
        catch (BucketNotFoundException bucketNotFoundException)
        {
            _logger.LogError(bucketNotFoundException, "Error when writing object to stream from bucket {Bucket} the object {Object}", bucketName, objectName);
            return new DownloadFileResult
            {
                IsFound = false
            };
        }
        catch (ObjectNotFoundException objectNotFoundException)
        {
            _logger.LogError(objectNotFoundException, "Error when writing object to stream from bucket {Bucket} the object {Object}", bucketName, objectName);
            return new DownloadFileResult
            {
                IsFound = false
            };
        }
    }
}

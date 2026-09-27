using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Common.Seaweed;

public interface ISeaweedContext
{
    Task<bool> EnsureBucketExists(string bucketName, CancellationToken ct);

    Task UploadObject(string bucketName, string objectName, string contentType, Stream dataStream, long dataLength, CancellationToken ct);

    Task<DownloadObjectResult> DownloadObjectToStream(string bucketName, string objectName, Action<ObjectMetadata> metadataReceived, Stream targetStream, CancellationToken ct);
}

public readonly struct ObjectMetadata
{
    public string ContentType { get; init; }
    public long ContentLength { get; init; }
}

public readonly struct DownloadObjectResult
{
    public bool IsFound { get; init; }
}

internal class SeaweedContext : ISeaweedContext
{
    private readonly ILogger _logger;
    private readonly SeaweedOptions _options;
    private readonly IAmazonS3 _s3Client;

    public SeaweedContext(
        ILogger<SeaweedContext> logger,
        IOptions<SeaweedOptions> options,
        IAmazonS3 s3Client)
    {
        _logger = logger;
        _options = options.Value;
        _s3Client = s3Client;
    }

    public async Task<bool> EnsureBucketExists(string bucketName, CancellationToken ct)
    {
        try
        {
            await _s3Client.PutBucketAsync(bucketName, ct);
            _logger.LogInformation("Bucket {Bucket} created successfully", bucketName);
            return true;
        }
        catch (AmazonS3Exception s3Exception) when (s3Exception.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning("Bucket {Bucket} already exists, skip creating", bucketName);
            return true;
        }
        catch (AmazonS3Exception s3Exception)
        {
            _logger.LogError(s3Exception, "Failed to create bucket {Bucket}", bucketName);
            return false;
        }
    }

    public async Task UploadObject(string bucketName, string objectName, string contentType, Stream dataStream, long dataLength, CancellationToken ct)
    {
        PutObjectRequest request = new()
        {
            BucketName = bucketName,
            Key = objectName,
            ContentType = contentType,
            InputStream = dataStream,
            AutoCloseStream = false, // caller owner owns the stream
        };
        request.Headers.ContentLength = dataLength;

        if (_options.Endpoint.Scheme == Uri.UriSchemeHttps)
        {
            request.UseChunkEncoding = false; // plain Content-Length body
            request.DisablePayloadSigning = true; // do not hash the body upfront
        }

        await _s3Client.PutObjectAsync(request, ct);
    }

    public async Task<DownloadObjectResult> DownloadObjectToStream(string bucketName, string objectName, Action<ObjectMetadata> metadataReceived, Stream targetStream, CancellationToken ct)
    {
        try
        {
            using GetObjectResponse response = await _s3Client.GetObjectAsync(bucketName, objectName, ct);

            ObjectMetadata objectMetadata = new()
            {
                ContentType = response.Headers.ContentType,
                ContentLength = response.Headers.ContentLength
            };
            metadataReceived(objectMetadata);

            await response.ResponseStream.CopyToAsync(targetStream, ct);
            return new DownloadObjectResult
            {
                IsFound = true
            };
        }
        catch (AmazonS3Exception s3Exception) when (s3Exception.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogError(s3Exception, "Unable to find object {Object} from bucket {Bucket} in order to download it", objectName, bucketName);

            return new DownloadObjectResult
            {
                IsFound = false
            };
        }
    }
}

using CecoChat.Bff.Service.Files;
using Common.AspNet.Init;
using Common.Minio;
using Microsoft.Extensions.Options;

namespace CecoChat.Bff.Service.Init;

public class FileStorageInit : InitStep
{
    private readonly IMinioContext _minio;
    private readonly MinioOptions _minioOptions;
    private readonly IObjectNaming _objectNaming;
    private readonly FileStorageInitHealthCheck _fileStorageInitHealthCheck;

    public FileStorageInit(
        IMinioContext minio,
        IOptions<MinioOptions> minioOptions,
        IObjectNaming objectNaming,
        FileStorageInitHealthCheck fileStorageInitHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _minio = minio;
        _minioOptions = minioOptions.Value;
        _objectNaming = objectNaming;
        _fileStorageInitHealthCheck = fileStorageInitHealthCheck;
    }

    protected override async Task<bool> DoExecute(CancellationToken ct)
    {
        await _minio.EnsureBucketExists(_minioOptions.HealthBucket, ct);

        string bucketName = _objectNaming.GetCurrentBucketName();
        _fileStorageInitHealthCheck.IsReady = await _minio.EnsureBucketExists(bucketName, ct);

        return _fileStorageInitHealthCheck.IsReady;
    }
}

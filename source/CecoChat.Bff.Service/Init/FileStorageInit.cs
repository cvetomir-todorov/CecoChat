using CecoChat.Bff.Service.Files;
using Common.AspNet.Init;
using Common.Seaweed;

namespace CecoChat.Bff.Service.Init;

// TODO: figure out how to have the buckets pre-built
// TODO: consider whether this is needed when there is a better health check
public class FileStorageInit : InitStep
{
    private readonly ISeaweedContext _seaweed;
    private readonly IObjectNaming _objectNaming;
    private readonly FileStorageInitHealthCheck _fileStorageInitHealthCheck;

    public FileStorageInit(
        ISeaweedContext seaweed,
        IObjectNaming objectNaming,
        FileStorageInitHealthCheck fileStorageInitHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _seaweed = seaweed;
        _objectNaming = objectNaming;
        _fileStorageInitHealthCheck = fileStorageInitHealthCheck;
    }

    protected override async Task<bool> DoExecute(CancellationToken ct)
    {
        //await _minio.EnsureBucketExists(_minioOptions.HealthBucket, ct);

        string bucketName = _objectNaming.GetCurrentBucketName();
        _fileStorageInitHealthCheck.IsReady = await _seaweed.EnsureBucketExists(bucketName, ct);

        return _fileStorageInitHealthCheck.IsReady;
    }
}

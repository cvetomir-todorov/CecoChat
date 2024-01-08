using CecoChat.AspNet.Init;
using CecoChat.Minio;
using CecoChat.Server.Bff.Files;

namespace CecoChat.Server.Bff.Init;

public class FileStorageInit : InitStep
{
    private readonly IMinioContext _minio;
    private readonly IObjectNaming _objectNaming;
    private readonly FileStorageInitHealthCheck _fileStorageInitHealthCheck;

    public FileStorageInit(
        IMinioContext minio,
        IObjectNaming objectNaming,
        FileStorageInitHealthCheck fileStorageInitHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _minio = minio;
        _objectNaming = objectNaming;
        _fileStorageInitHealthCheck = fileStorageInitHealthCheck;
    }

    protected override async Task<bool> DoExecute(CancellationToken ct)
    {
        string bucketName = _objectNaming.GetCurrentBucketName();
        _fileStorageInitHealthCheck.IsReady = await _minio.EnsureBucketExists(bucketName, ct);

        return _fileStorageInitHealthCheck.IsReady;
    }
}

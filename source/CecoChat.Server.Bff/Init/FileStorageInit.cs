using CecoChat.AspNet.Init;
using CecoChat.Minio;
using CecoChat.Server.Bff.Files;

namespace CecoChat.Server.Bff.Init;

public class FileStorageInit : InitStep
{
    private readonly IMinioContext _minio;
    private readonly IFileStorage _fileStorage;
    private readonly FileStorageInitHealthCheck _fileStorageInitHealthCheck;

    public FileStorageInit(
        IMinioContext minio,
        IFileStorage fileStorage,
        FileStorageInitHealthCheck fileStorageInitHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _minio = minio;
        _fileStorage = fileStorage;
        _fileStorageInitHealthCheck = fileStorageInitHealthCheck;
    }

    protected override async Task<bool> DoExecute(CancellationToken ct)
    {
        string bucketName = _fileStorage.GetCurrentBucketName();
        _fileStorageInitHealthCheck.IsReady = await _minio.EnsureBucketExists(bucketName, ct);

        return _fileStorageInitHealthCheck.IsReady;
    }
}

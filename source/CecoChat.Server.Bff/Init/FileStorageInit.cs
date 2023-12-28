using CecoChat.AspNet.Init;
using CecoChat.Minio;

namespace CecoChat.Server.Bff.Init;

public class FileStorageInit : InitStep
{
    private readonly IMinioContext _minio;
    private readonly IClock _clock;
    private readonly FileStorageInitHealthCheck _fileStorageInitHealthCheck;

    public FileStorageInit(
        IMinioContext minio,
        IClock clock,
        FileStorageInitHealthCheck fileStorageInitHealthCheck,
        IHostApplicationLifetime applicationLifetime)
        : base(applicationLifetime)
    {
        _minio = minio;
        _clock = clock;
        _fileStorageInitHealthCheck = fileStorageInitHealthCheck;
    }

    protected override async Task<bool> DoExecute(CancellationToken ct)
    {
        DateTime now = _clock.GetNowUtc();
        string bucketName = $"files-{now.Year}-{now.Month:00}-{now.Day:00}";

        _fileStorageInitHealthCheck.IsReady = await _minio.EnsureBucketExists(bucketName, ct);

        return _fileStorageInitHealthCheck.IsReady;
    }
}

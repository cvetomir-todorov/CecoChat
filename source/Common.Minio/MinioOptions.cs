namespace Common.Minio;

public sealed class MinioOptions
{
    public Uri Endpoint { get; init; } = null!;
    public string AccessKey { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;

    public string HealthBucket { get; init; } = string.Empty;
    public TimeSpan HealthTimeout { get; init; }
}

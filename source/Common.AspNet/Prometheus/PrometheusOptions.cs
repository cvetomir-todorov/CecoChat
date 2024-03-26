namespace Common.AspNet.Prometheus;

public sealed class PrometheusOptions
{
    public string ScrapeEndpointPath { get; init; } = string.Empty;

    // 0 means disable response caching
    public int ScrapeResponseCacheDurationMilliseconds { get; init; }
}

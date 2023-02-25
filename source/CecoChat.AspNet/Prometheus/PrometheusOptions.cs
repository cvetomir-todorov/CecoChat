namespace CecoChat.AspNet.Prometheus;

public sealed class PrometheusOptions
{
    public string ScrapeEndpointPath { get; set; } = string.Empty;

    // 0 means disable response caching
    public int ScrapeResponseCacheDurationMilliseconds { get; set; }
}

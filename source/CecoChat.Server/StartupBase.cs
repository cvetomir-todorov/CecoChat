using CecoChat.AspNet.Prometheus;
using CecoChat.Jwt;
using CecoChat.Otel;
using CecoChat.Redis;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Server;

public abstract class StartupBase
{
    protected StartupBase(IConfiguration configuration)
    {
        Configuration = configuration;

        ConfigDbOptions = new();
        Configuration.GetSection("ConfigDb").Bind(ConfigDbOptions);

        JwtOptions = new();
        Configuration.GetSection("Jwt").Bind(JwtOptions);

        TracingSamplingOptions = new();
        Configuration.GetSection("Telemetry:Tracing:Sampling").Bind(TracingSamplingOptions);

        TracingExportOptions = new();
        Configuration.GetSection("Telemetry:Tracing:Export").Bind(TracingExportOptions);

        PrometheusOptions = new();
        Configuration.GetSection("Telemetry:Metrics:Prometheus").Bind(PrometheusOptions);
    }

    protected IConfiguration Configuration { get; }
    protected RedisOptions ConfigDbOptions { get; }
    protected JwtOptions JwtOptions { get; }
    protected OtelSamplingOptions TracingSamplingOptions { get; }
    protected OtlpOptions TracingExportOptions { get; }
    protected PrometheusOptions PrometheusOptions { get; }
}

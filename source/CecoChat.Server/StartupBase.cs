using CecoChat.AspNet.Prometheus;
using CecoChat.Client.Config;
using CecoChat.Jwt;
using CecoChat.Otel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Server;

public abstract class StartupBase
{
    protected StartupBase(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;

        ConfigClientOptions = new();
        Configuration.GetSection("ConfigClient").Bind(ConfigClientOptions);

        JwtOptions = new();
        Configuration.GetSection("Jwt").Bind(JwtOptions);

        TracingSamplingOptions = new();
        Configuration.GetSection("Telemetry:Tracing:Sampling").Bind(TracingSamplingOptions);

        TracingExportOptions = new();
        Configuration.GetSection("Telemetry:Tracing:Export").Bind(TracingExportOptions);

        PrometheusOptions = new();
        Configuration.GetSection("Telemetry:Metrics:Prometheus").Bind(PrometheusOptions);
    }

    protected IWebHostEnvironment Environment { get; }
    protected IConfiguration Configuration { get; }
    protected ConfigClientOptions ConfigClientOptions { get; }
    protected JwtOptions JwtOptions { get; }
    protected OtelSamplingOptions TracingSamplingOptions { get; }
    protected OtlpOptions TracingExportOptions { get; }
    protected PrometheusOptions PrometheusOptions { get; }
}

using CecoChat.Config.Client;
using Common.AspNet.Prometheus;
using Common.Jwt;
using Common.OpenTelemetry;
using Microsoft.Extensions.Configuration;

namespace CecoChat.Server;

public sealed class CommonOptions
{
    public CommonOptions(IConfiguration configuration)
    {
        ConfigClient = new();
        configuration.GetSection("ConfigClient").Bind(ConfigClient);

        Jwt = new();
        configuration.GetSection("Jwt").Bind(Jwt);

        TracingSampling = new();
        configuration.GetSection("Telemetry:Tracing:Sampling").Bind(TracingSampling);

        TracingExport = new();
        configuration.GetSection("Telemetry:Tracing:Export").Bind(TracingExport);

        Prometheus = new();
        configuration.GetSection("Telemetry:Metrics:Prometheus").Bind(Prometheus);
    }

    public ConfigClientOptions ConfigClient { get; }
    public JwtOptions Jwt { get; }
    public OtelSamplingOptions TracingSampling { get; }
    public OtlpExporterOptions TracingExport { get; }
    public PrometheusOptions Prometheus { get; }
}

using Autofac;
using CecoChat.Autofac;
using CecoChat.Contracts.Backplane;
using CecoChat.Data.Config;
using CecoChat.Data.History;
using CecoChat.Data.History.Telemetry;
using CecoChat.Jwt;
using CecoChat.Kafka;
using CecoChat.Kafka.Telemetry;
using CecoChat.Otel;
using CecoChat.Server.History.Backplane;
using CecoChat.Server.History.Clients;
using CecoChat.Server.History.HostedServices;
using CecoChat.Server.Identity;
using Confluent.Kafka;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.History;

public class Startup
{
    private readonly JwtOptions _jwtOptions;
    private readonly OtelSamplingOptions _otelSamplingOptions;
    private readonly JaegerOptions _jaegerOptions;
    private readonly PrometheusOptions _prometheusOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;

        _jwtOptions = new();
        Configuration.GetSection("Jwt").Bind(_jwtOptions);

        _otelSamplingOptions = new();
        Configuration.GetSection("OtelSampling").Bind(_otelSamplingOptions);

        _jaegerOptions = new();
        Configuration.GetSection("Jaeger").Bind(_jaegerOptions);

        _prometheusOptions = new();
        Configuration.GetSection("Prometheus").Bind(_prometheusOptions);
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment Environment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        // telemetry
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "History", serviceNamespace: "CecoChat", serviceVersion: "0.1")
            .AddEnvironmentVariableDetector();

        services.AddOpenTelemetryTracing(tracing =>
        {
            tracing.SetResourceBuilder(serviceResourceBuilder);
            tracing.AddAspNetCoreInstrumentation(aspnet =>
            {
                aspnet.EnableGrpcAspNetCoreSupport = true;
                aspnet.Filter = httpContext => httpContext.Request.Path != _prometheusOptions.ScrapeEndpointPath;
            });
            tracing.AddKafkaInstrumentation();
            tracing.AddHistoryInstrumentation();
            tracing.ConfigureSampling(_otelSamplingOptions);
            tracing.ConfigureJaegerExporter(_jaegerOptions);
        });
        services.AddOpenTelemetryMetrics(metrics =>
        {
            metrics.SetResourceBuilder(serviceResourceBuilder);
            metrics.AddAspNetCoreInstrumentation();
            metrics.AddHistoryInstrumentation();
            metrics.ConfigurePrometheusAspNetExporter(_prometheusOptions);
        });

        // security
        services.AddJwtAuthentication(_jwtOptions);
        services.AddAuthorization();

        // clients
        services.AddGrpc(rpc => rpc.EnableDetailedErrors = Environment.IsDevelopment());

        // required
        services.AddOptions();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitDynamicConfig>();
        builder.RegisterHostedService<InitHistoryDb>();
        builder.RegisterHostedService<StartMaterializeMessages>();

        // configuration
        IConfiguration configDbConfig = Configuration.GetSection("ConfigDB");
        builder.RegisterModule(new ConfigDbAutofacModule(configDbConfig, registerHistory: true));

        // history
        IConfiguration historyDbConfig = Configuration.GetSection("HistoryDB");
        builder.RegisterModule(new HistoryDbAutofacModule(historyDbConfig));

        // backplane
        builder.RegisterType<HistoryConsumer>().As<IHistoryConsumer>().SingleInstance();
        builder.RegisterFactory<KafkaConsumer<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
        builder.RegisterModule(new KafkaAutofacModule());
        builder.RegisterOptions<BackplaneOptions>(Configuration.GetSection("Backplane"));

        // shared
        builder.RegisterType<ContractDataMapper>().As<IContractDataMapper>().SingleInstance();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<HistoryService>();
        });

        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == _prometheusOptions.ScrapeEndpointPath);
    }
}

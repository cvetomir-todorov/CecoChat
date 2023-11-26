using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.AspNet.Health;
using CecoChat.AspNet.Prometheus;
using CecoChat.Autofac;
using CecoChat.Data.Config;
using CecoChat.Otel;
using CecoChat.Redis;
using CecoChat.Redis.Health;
using CecoChat.Server.IdGen.Endpoints;
using CecoChat.Server.IdGen.HostedServices;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.IdGen;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly RedisOptions _configDbOptions;
    private readonly OtelSamplingOptions _tracingSamplingOptions;
    private readonly OtlpOptions _tracingExportOptions;
    private readonly PrometheusOptions _prometheusOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;

        _configDbOptions = new();
        _configuration.GetSection("ConfigDb").Bind(_configDbOptions);

        _tracingSamplingOptions = new();
        _configuration.GetSection("Telemetry:Tracing:Sampling").Bind(_tracingSamplingOptions);

        _tracingExportOptions = new();
        _configuration.GetSection("Telemetry:Tracing:Export").Bind(_tracingExportOptions);

        _prometheusOptions = new();
        _configuration.GetSection("Telemetry:Metrics:Prometheus").Bind(_prometheusOptions);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        AddTelemetryServices(services);
        AddHealthServices(services);

        // clients
        services.AddGrpc(grpc =>
        {
            grpc.EnableDetailedErrors = _environment.IsDevelopment();
            grpc.EnableMessageValidation();
        });
        services.AddGrpcValidation();

        // common
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddOptions();
    }

    private void AddTelemetryServices(IServiceCollection services)
    {
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "IdGen", serviceNamespace: "CecoChat", serviceVersion: "0.1")
            .AddEnvironmentVariableDetector();

        services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(serviceResourceBuilder);
                tracing.AddAspNetCoreInstrumentation(aspnet =>
                {
                    aspnet.EnableGrpcAspNetCoreSupport = true;
                    HashSet<string> excludedPaths = new()
                    {
                        _prometheusOptions.ScrapeEndpointPath, HealthPaths.Health, HealthPaths.Startup, HealthPaths.Live, HealthPaths.Ready
                    };
                    aspnet.Filter = httpContext => !excludedPaths.Contains(httpContext.Request.Path);
                });
                tracing.ConfigureSampling(_tracingSamplingOptions);
                tracing.ConfigureOtlpExporter(_tracingExportOptions);
            })
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(serviceResourceBuilder);
                metrics.AddAspNetCoreInstrumentation();
                metrics.ConfigurePrometheusAspNetExporter(_prometheusOptions);
            });
    }

    private void AddHealthServices(IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck<ConfigDbInitHealthCheck>(
                "config-db-init",
                tags: new[] { HealthTags.Health, HealthTags.Startup })
            .AddRedis(
                "config-db",
                _configDbOptions,
                tags: new[] { HealthTags.Health, HealthTags.Ready });

        services.AddSingleton<ConfigDbInitHealthCheck>();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitDynamicConfig>();

        // configuration
        IConfiguration configDbConfig = _configuration.GetSection("ConfigDb");
        builder.RegisterModule(new ConfigDbAutofacModule(configDbConfig, registerSnowflake: true));
        builder.RegisterOptions<ConfigOptions>(_configuration.GetSection("Config"));

        // snowflake
        builder.RegisterType<SnowflakeGenerator>().As<IIdentityGenerator>().SingleInstance();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<IdGenService>();
            endpoints.MapHttpHealthEndpoints(setup =>
            {
                Func<HttpContext, HealthReport, Task> responseWriter = (context, report) => CustomHealth.Writer(serviceName: "idgen", context, report);
                setup.Health.ResponseWriter = responseWriter;

                if (env.IsDevelopment())
                {
                    setup.Startup.ResponseWriter = responseWriter;
                    setup.Live.ResponseWriter = responseWriter;
                    setup.Ready.ResponseWriter = responseWriter;
                }
            });
        });

        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == _prometheusOptions.ScrapeEndpointPath);
    }
}

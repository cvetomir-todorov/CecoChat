using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.AspNet.Health;
using CecoChat.AspNet.Init;
using CecoChat.AspNet.Prometheus;
using CecoChat.Autofac;
using CecoChat.Client.Config;
using CecoChat.DynamicConfig;
using CecoChat.Kafka;
using CecoChat.Kafka.Telemetry;
using CecoChat.Otel;
using CecoChat.Server.Backplane;
using CecoChat.Server.IdGen.Endpoints;
using CecoChat.Server.IdGen.Init;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.IdGen;

public static class Program
{
    public static async Task Main(params string[] args)
    {
        WebApplicationBuilder builder = EntryPoint.CreateWebAppBuilder(args);
        CommonOptions options = new(builder.Configuration);
        
        ConfigureServices(builder, options);
        builder.Host.ConfigureContainer<ContainerBuilder>(ConfigureContainer);

        WebApplication app = builder.Build();
        ConfigurePipeline(app, options);
        await EntryPoint.RunWebApp(app, typeof(Program));
    }

    private static void ConfigureServices(WebApplicationBuilder builder, CommonOptions options)
    {
        AddTelemetry(builder, options);
        AddHealth(builder, options);

        // dynamic config
        builder.Services.AddConfigClient(options.ConfigClient);

        // grpc
        builder.Services.AddGrpc(grpc =>
        {
            grpc.EnableDetailedErrors = builder.Environment.IsDevelopment();
            grpc.EnableMessageValidation();
        });
        builder.Services.AddGrpcValidation();

        // common
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddOptions();
    }

    private static void AddTelemetry(WebApplicationBuilder builder, CommonOptions options)
    {
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "IdGen", serviceNamespace: "CecoChat", serviceVersion: "0.1")
            .AddEnvironmentVariableDetector();

        builder.Services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(serviceResourceBuilder)
                    .AddAspNetCoreServer(enableGrpcSupport: true, options.Prometheus)
                    .AddKafkaInstrumentation()
                    .AddGrpcClientInstrumentation(grpc => grpc.SuppressDownstreamInstrumentation = true)
                    .ConfigureSampling(options.TracingSampling)
                    .ConfigureOtlpExporter(options.TracingExport);
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(serviceResourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .ConfigurePrometheusAspNetExporter(options.Prometheus);
            });
    }

    private static void AddHealth(WebApplicationBuilder builder, CommonOptions options)
    {
        builder.Services
            .AddHealthChecks()
            .AddDynamicConfigInit()
            .AddConfigChangesConsumer()
            .AddConfigService(options.ConfigClient)
            .AddBackplane(builder.Configuration.GetSection("Backplane"));
    }

    private static void ConfigureContainer(HostBuilderContext host, ContainerBuilder builder)
    {
        // init
        builder.RegisterInitStep<DynamicConfigInit>();
        builder.RegisterInitStep<BackplaneInit>();
        builder.RegisterInitStep<BackplaneComponentsInit>();

        // config
        builder.RegisterOptions<ConfigOptions>(host.Configuration.GetSection("Config"));

        // dynamic config
        builder.RegisterModule(new DynamicConfigAutofacModule(
            host.Configuration.GetSection("Backplane"),
            registerConfigChangesConsumer: true,
            registerSnowflake: true));
        builder.RegisterModule(new ConfigClientAutofacModule(host.Configuration.GetSection("ConfigClient")));

        // backplane
        builder.RegisterType<KafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
        builder.RegisterOptions<KafkaOptions>(host.Configuration.GetSection("Backplane:Kafka"));

        // snowflake
        builder.RegisterType<SnowflakeGenerator>().As<IIdentityGenerator>().SingleInstance();

        // shared
        builder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
    }

    private static void ConfigurePipeline(WebApplication app, CommonOptions options)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCustomExceptionHandler();
        app.UseHttpsRedirection();

        app.UseRouting();

        app.MapGrpcService<IdGenService>();
        app.MapHttpHealthEndpoints(setup =>
        {
            Func<HttpContext, HealthReport, Task> responseWriter = (context, report) => CustomHealth.Writer(serviceName: "idgen", context, report);
            setup.Health.ResponseWriter = responseWriter;

            if (app.Environment.IsDevelopment())
            {
                setup.Startup.ResponseWriter = responseWriter;
                setup.Live.ResponseWriter = responseWriter;
                setup.Ready.ResponseWriter = responseWriter;
            }
        });

        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == options.Prometheus.ScrapeEndpointPath);
    }
}

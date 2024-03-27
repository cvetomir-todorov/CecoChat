using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.Client.Config;
using CecoChat.DynamicConfig;
using CecoChat.IdGen.Service.Endpoints;
using CecoChat.IdGen.Service.Init;
using CecoChat.Server;
using CecoChat.Server.Backplane;
using Common;
using Common.AspNet.Init;
using Common.AspNet.Prometheus;
using Common.Autofac;
using Common.Kafka;
using Common.Kafka.Telemetry;
using Common.OpenTelemetry;
using FluentValidation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.IdGen.Service;

public static class Program
{
    public static async Task Main(params string[] args)
    {
        WebApplicationBuilder builder = EntryPoint.CreateWebAppBuilder(args);
        CommonOptions options = new(builder.Configuration);

        AddServices(builder, options);
        AddTelemetry(builder, options);
        AddHealth(builder, options);
        builder.Host.ConfigureContainer<ContainerBuilder>(ConfigureContainer);

        WebApplication app = builder.Build();
        ConfigurePipeline(app, options);
        await EntryPoint.RunWebApp(app, typeof(Program));
    }

    private static void AddServices(WebApplicationBuilder builder, CommonOptions options)
    {
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
                builder.EnableGrpcInstrumentationForAspNet();
                tracing
                    .SetResourceBuilder(serviceResourceBuilder)
                    .AddAspNetCoreServer(options.Prometheus)
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
        app.MapCustomHttpHealthEndpoints(app.Environment, serviceName: "idgen");

        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == options.Prometheus.ScrapeEndpointPath);
    }
}

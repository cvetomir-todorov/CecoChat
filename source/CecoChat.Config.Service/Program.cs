using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.Backplane;
using CecoChat.Config.Data;
using CecoChat.Config.Service.Endpoints;
using CecoChat.Config.Service.Init;
using CecoChat.Server;
using Common.AspNet.FluentValidation;
using Common.AspNet.Health;
using Common.AspNet.Init;
using Common.AspNet.ModelBinding;
using Common.AspNet.Prometheus;
using Common.AspNet.Swagger;
using Common.Autofac;
using Common.Kafka;
using Common.Kafka.Telemetry;
using Common.Npgsql;
using Common.Npgsql.Health;
using Common.OpenTelemetry;
using FluentValidation;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Config.Service;

public static class Program
{
    private static ConfigDbOptions _configDbOptions = null!;
    private static SwaggerOptions _swaggerOptions = null!;

    public static async Task<int> Main(params string[] args)
    {
        return await EntryPoint.Run(args, typeof(Program), ConfigureBuilder, ConfigurePipeline);
    }

    private static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        CommonOptions commonOptions = new(builder.Configuration);

        _configDbOptions = new();
        builder.Configuration.GetSection("ConfigDb").Bind(_configDbOptions);
        _swaggerOptions = new();
        builder.Configuration.GetSection("Swagger").Bind(_swaggerOptions);

        AddServices(builder, commonOptions);
        AddTelemetry(builder, commonOptions);
        AddHealth(builder);

        builder.Host.ConfigureContainer<ContainerBuilder>(ConfigureContainer);
    }

    private static void AddServices(WebApplicationBuilder builder, CommonOptions commonOptions)
    {
        // grpc
        builder.Services.AddGrpc(grpc =>
        {
            grpc.EnableDetailedErrors = builder.Environment.IsDevelopment();
            grpc.EnableMessageValidation();
        });
        builder.Services.AddGrpcValidation();

        // rest
        builder.Services
            .AddControllers(mvc =>
            {
                // insert it before the default one so that it takes effect
                mvc.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
                mvc.AddFluentValidationAutoValidation();
            })
            .DisableDataAnnotationsValidation();
        builder.Services.AddSwaggerServices(_swaggerOptions);

        // config db
        builder.Services.AddConfigDb(_configDbOptions.Connect);

        // common
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddOptions();
        builder.Services.AddSingleton(commonOptions);
    }

    private static void AddTelemetry(WebApplicationBuilder builder, CommonOptions options)
    {
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "Config", serviceNamespace: "CecoChat", serviceVersion: "0.1")
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
                    .AddNpgsql()
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

    private static void AddHealth(WebApplicationBuilder builder)
    {
        builder.Services
            .AddHealthChecks()
            .AddBackplane(builder.Configuration.GetSection("Backplane"))
            .AddCheck<ConfigDbInitHealthCheck>(
                "config-db-init",
                tags: [HealthTags.Health, HealthTags.Startup])
            .AddNpgsql(
                "config-db",
                _configDbOptions.Connect,
                tags: [HealthTags.Health, HealthTags.Ready]);

        builder.Services.AddSingleton<ConfigDbInitHealthCheck>();
    }

    private static void ConfigureContainer(HostBuilderContext host, ContainerBuilder builder)
    {
        // init
        builder.RegisterInitStep<ConfigDbInit>();
        builder.RegisterInitStep<BackplaneInit>();

        // config db
        builder.RegisterType<NpgsqlDbInitializer>().As<INpgsqlDbInitializer>().SingleInstance();
        builder.RegisterOptions<ConfigDbOptions>(host.Configuration.GetSection("ConfigDb"));

        // dynamic config
        builder.RegisterModule(new DynamicConfigAutofacModule(
            host.Configuration.GetSection("Backplane"),
            registerConfigChangesProducer: true));

        // backplane
        builder.RegisterType<KafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
        builder.RegisterOptions<KafkaOptions>(host.Configuration.GetSection("Backplane:Kafka"));
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCustomExceptionHandler();
        app.UseHttpsRedirection();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGrpcService<ConfigService>();
        app.MapControllers();
        app.MapCustomHttpHealthEndpoints(app.Environment, serviceName: "config");

        CommonOptions commonOptions = app.Services.GetRequiredService<CommonOptions>();
        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == commonOptions.Prometheus.ScrapeEndpointPath);
        app.MapWhen(context => context.Request.Path.StartsWithSegments("/swagger"), _ =>
        {
            app.UseSwaggerMiddlewares(_swaggerOptions);
        });
    }
}

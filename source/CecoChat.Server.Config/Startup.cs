using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.AspNet.Health;
using CecoChat.AspNet.ModelBinding;
using CecoChat.AspNet.Prometheus;
using CecoChat.AspNet.Swagger;
using CecoChat.Autofac;
using CecoChat.Data.Config;
using CecoChat.DynamicConfig;
using CecoChat.Kafka;
using CecoChat.Kafka.Health;
using CecoChat.Kafka.Telemetry;
using CecoChat.Npgsql;
using CecoChat.Npgsql.Health;
using CecoChat.Otel;
using CecoChat.Server.Backplane;
using CecoChat.Server.Config.Backplane;
using CecoChat.Server.Config.Endpoints;
using CecoChat.Server.Config.HostedServices;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.Config;

public class Startup : StartupBase
{
    private readonly ConfigDbOptions _configDbOptions;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly SwaggerOptions _swaggerOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        : base(configuration, environment)
    {
        _configDbOptions = new();
        Configuration.GetSection("ConfigDb").Bind(_configDbOptions);

        _backplaneOptions = new();
        Configuration.GetSection("Backplane").Bind(_backplaneOptions);

        _swaggerOptions = new();
        Configuration.GetSection("Swagger").Bind(_swaggerOptions);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        AddTelemetryServices(services);
        AddHealthServices(services);

        // clients
        services.AddGrpc(grpc =>
        {
            grpc.EnableDetailedErrors = Environment.IsDevelopment();
            grpc.EnableMessageValidation();
        });
        services.AddGrpcValidation();

        // web
        services.AddControllers(mvc =>
        {
            // insert it before the default one so that it takes effect
            mvc.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
        });
        services.AddSwaggerServices(_swaggerOptions);

        // config db
        services.AddConfigDb(_configDbOptions.Connect);

        // common
        services.AddFluentValidationAutoValidation(fluentValidation =>
        {
            fluentValidation.DisableDataAnnotationsValidation = true;
        });
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }

    private void AddTelemetryServices(IServiceCollection services)
    {
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "Admin", serviceNamespace: "CecoChat", serviceVersion: "0.1")
            .AddEnvironmentVariableDetector();

        services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(serviceResourceBuilder);
                tracing.AddAspNetCoreInstrumentation(aspnet =>
                {
                    HashSet<string> excludedPaths = new()
                    {
                        PrometheusOptions.ScrapeEndpointPath, HealthPaths.Health, HealthPaths.Startup, HealthPaths.Live, HealthPaths.Ready
                    };
                    aspnet.Filter = httpContext => !excludedPaths.Contains(httpContext.Request.Path);
                });
                tracing.AddKafkaInstrumentation();
                tracing.AddNpgsql();
                tracing.ConfigureSampling(TracingSamplingOptions);
                tracing.ConfigureOtlpExporter(TracingExportOptions);
            })
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(serviceResourceBuilder);
                metrics.AddAspNetCoreInstrumentation();
                metrics.ConfigurePrometheusAspNetExporter(PrometheusOptions);
            });
    }

    private void AddHealthServices(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<ConfigDbInitHealthCheck>(
                "config-db-init",
                tags: new[] { HealthTags.Health, HealthTags.Startup })
            .AddNpgsql(
                "config-db",
                _configDbOptions.Connect,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddKafka(
                "backplane",
                _backplaneOptions.Kafka,
                _backplaneOptions.Health,
                tags: new[] { HealthTags.Health });

        services.AddSingleton<ConfigDbInitHealthCheck>();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitConfigDb>();
        builder.RegisterHostedService<InitBackplane>();

        // config db
        builder.RegisterType<NpgsqlDbInitializer>().As<INpgsqlDbInitializer>().SingleInstance();
        IConfiguration configDbConfig = Configuration.GetSection("ConfigDb");
        builder.RegisterOptions<ConfigDbOptions>(configDbConfig);

        // dynamic config
        IConfiguration backplaneConfiguration = Configuration.GetSection("Backplane");
        builder.RegisterModule(new DynamicConfigAutofacModule(backplaneConfiguration, registerConfigChangesProducer: true));

        // backplane
        builder.RegisterType<KafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
        builder.RegisterOptions<KafkaOptions>(Configuration.GetSection("Backplane:Kafka"));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseCustomExceptionHandler();
        app.UseHttpsRedirection();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<ConfigService>();
            endpoints.MapControllers();
            endpoints.MapHttpHealthEndpoints(setup =>
            {
                Func<HttpContext, HealthReport, Task> responseWriter = (context, report) => CustomHealth.Writer(serviceName: "admin", context, report);
                setup.Health.ResponseWriter = responseWriter;

                if (env.IsDevelopment())
                {
                    setup.Startup.ResponseWriter = responseWriter;
                    setup.Live.ResponseWriter = responseWriter;
                    setup.Ready.ResponseWriter = responseWriter;
                }
            });
        });

        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == PrometheusOptions.ScrapeEndpointPath);
        app.MapWhen(context => context.Request.Path.StartsWithSegments("/swagger"), _ =>
        {
            app.UseSwaggerMiddlewares(_swaggerOptions);
        });
    }
}
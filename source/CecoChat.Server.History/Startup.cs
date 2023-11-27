using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.AspNet.Health;
using CecoChat.AspNet.Prometheus;
using CecoChat.Autofac;
using CecoChat.Cassandra;
using CecoChat.Cassandra.Health;
using CecoChat.Contracts.Backplane;
using CecoChat.Data.Config;
using CecoChat.Data.History;
using CecoChat.Data.History.Telemetry;
using CecoChat.Jwt;
using CecoChat.Kafka;
using CecoChat.Kafka.Health;
using CecoChat.Kafka.Telemetry;
using CecoChat.Otel;
using CecoChat.Redis;
using CecoChat.Redis.Health;
using CecoChat.Server.Backplane;
using CecoChat.Server.History.Backplane;
using CecoChat.Server.History.Endpoints;
using CecoChat.Server.History.HostedServices;
using CecoChat.Server.Identity;
using Confluent.Kafka;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.History;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly RedisOptions _configDbOptions;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly CassandraOptions _historyDbOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly OtelSamplingOptions _tracingSamplingOptions;
    private readonly OtlpOptions _tracingExportOptions;
    private readonly PrometheusOptions _prometheusOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;

        _configDbOptions = new();
        _configuration.GetSection("ConfigDb").Bind(_configDbOptions);

        _backplaneOptions = new();
        _configuration.GetSection("Backplane").Bind(_backplaneOptions);

        _historyDbOptions = new();
        _configuration.GetSection("HistoryDb").Bind(_historyDbOptions);

        _jwtOptions = new();
        _configuration.GetSection("Jwt").Bind(_jwtOptions);

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

        // security
        services.AddJwtAuthentication(_jwtOptions);
        services.AddUserPolicyAuthorization();

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
            .AddService(serviceName: "History", serviceNamespace: "CecoChat", serviceVersion: "0.1")
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
                tracing.AddKafkaInstrumentation();
                tracing.AddHistoryInstrumentation();
                tracing.ConfigureSampling(_tracingSamplingOptions);
                tracing.ConfigureOtlpExporter(_tracingExportOptions);
            })
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(serviceResourceBuilder);
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHistoryInstrumentation();
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
            .AddCheck<HistoryDbInitHealthCheck>(
                "history-db-init",
                tags: new[] { HealthTags.Health, HealthTags.Startup })
            .AddCheck<HistoryConsumerHealthCheck>(
                "history-consumer",
                tags: new[] { HealthTags.Health, HealthTags.Startup, HealthTags.Live })
            .AddRedis(
                "config-db",
                _configDbOptions,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddCassandra(
                name: "history-db",
                timeout: _historyDbOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddKafka(
                "backplane",
                _backplaneOptions.Kafka,
                _backplaneOptions.Health,
                tags: new[] { HealthTags.Health });

        services.AddSingleton<ConfigDbInitHealthCheck>();
        services.AddSingleton<HistoryDbInitHealthCheck>();
        services.AddSingleton<HistoryConsumerHealthCheck>();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitDynamicConfig>();
        builder.RegisterHostedService<InitHistoryDb>();
        builder.RegisterHostedService<InitBackplane>();
        builder.RegisterHostedService<InitBackplaneComponents>();

        // configuration
        IConfiguration configDbConfig = _configuration.GetSection("ConfigDb");
        builder.RegisterModule(new ConfigDbAutofacModule(configDbConfig, registerHistory: true));

        // history db
        IConfiguration historyDbConfig = _configuration.GetSection("HistoryDb");
        builder.RegisterModule(new HistoryDbAutofacModule(historyDbConfig));
        builder.RegisterModule(new CassandraHealthAutofacModule(historyDbConfig));

        // backplane
        builder.RegisterType<KafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
        builder.RegisterOptions<KafkaOptions>(_configuration.GetSection("Backplane:Kafka"));
        builder.RegisterType<HistoryConsumer>().As<IHistoryConsumer>().SingleInstance();
        builder.RegisterFactory<KafkaConsumer<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
        builder.RegisterModule(new KafkaAutofacModule());
        builder.RegisterOptions<BackplaneOptions>(_configuration.GetSection("Backplane"));

        // shared
        builder.RegisterType<ContractMapper>().As<IContractMapper>().SingleInstance();
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
            endpoints.MapHttpHealthEndpoints(setup =>
            {
                Func<HttpContext, HealthReport, Task> responseWriter = (context, report) => CustomHealth.Writer(serviceName: "history", context, report);
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

using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.AspNet.Health;
using CecoChat.AspNet.Prometheus;
using CecoChat.Autofac;
using CecoChat.Cassandra;
using CecoChat.Cassandra.Health;
using CecoChat.Contracts.Backplane;
using CecoChat.Data.State;
using CecoChat.Data.State.Telemetry;
using CecoChat.Jwt;
using CecoChat.Kafka;
using CecoChat.Kafka.Health;
using CecoChat.Kafka.Telemetry;
using CecoChat.Otel;
using CecoChat.Redis;
using CecoChat.Redis.Health;
using CecoChat.Server.Identity;
using CecoChat.Server.State.Backplane;
using CecoChat.Server.State.Endpoints;
using CecoChat.Server.State.HostedServices;
using Confluent.Kafka;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.State;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly RedisOptions _configDbOptions;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly CassandraOptions _stateDbOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly OtelSamplingOptions _tracingSamplingOptions;
    private readonly OtlpOptions _tracingExportOptions;
    private readonly PrometheusOptions _prometheusOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;

        _configDbOptions = new();
        configuration.GetSection("ConfigDb").Bind(_configDbOptions);

        _backplaneOptions = new();
        configuration.GetSection("Backplane").Bind(_backplaneOptions);

        _stateDbOptions = new();
        configuration.GetSection("StateDb").Bind(_stateDbOptions);

        _jwtOptions = new();
        configuration.GetSection("Jwt").Bind(_jwtOptions);

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
            .AddService(serviceName: "State", serviceNamespace: "CecoChat", serviceVersion: "0.1")
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
                tracing.AddStateInstrumentation();
                tracing.ConfigureSampling(_tracingSamplingOptions);
                tracing.ConfigureOtlpExporter(_tracingExportOptions);
            })
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(serviceResourceBuilder);
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddStateInstrumentation();
                metrics.ConfigurePrometheusAspNetExporter(_prometheusOptions);
            });
    }

    private void AddHealthServices(IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck<StateDbInitHealthCheck>(
                "state-db-init",
                tags: new[] { HealthTags.Health, HealthTags.Startup })
            .AddCheck<ReceiversConsumerHealthCheck>(
                "receivers-consumer",
                tags: new[] { HealthTags.Health, HealthTags.Startup, HealthTags.Live })
            .AddCheck<SendersConsumerHealthCheck>(
                "senders-consumer",
                tags: new[] { HealthTags.Health, HealthTags.Startup, HealthTags.Live })
            .AddRedis(
                "config-db",
                _configDbOptions,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddCassandra(
                name: "state-db",
                timeout: _stateDbOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddKafka(
                "backplane",
                _backplaneOptions.Kafka,
                _backplaneOptions.Health,
                tags: new[] { HealthTags.Health });

        services.AddSingleton<StateDbInitHealthCheck>();
        services.AddSingleton<ReceiversConsumerHealthCheck>();
        services.AddSingleton<SendersConsumerHealthCheck>();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitStateDb>();
        builder.RegisterHostedService<InitBackplaneComponents>();

        // state db
        IConfiguration stateDbConfig = _configuration.GetSection("StateDb");
        builder.RegisterModule(new StateDbAutofacModule(stateDbConfig));
        builder.RegisterModule(new CassandraHealthAutofacModule(stateDbConfig));

        // backplane
        builder.RegisterType<StateConsumer>().As<IStateConsumer>().SingleInstance();
        builder.RegisterFactory<KafkaConsumer<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
        builder.RegisterModule(new KafkaAutofacModule());
        builder.RegisterOptions<BackplaneOptions>(_configuration.GetSection("Backplane"));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<StateService>();
            endpoints.MapHttpHealthEndpoints(setup =>
            {
                Func<HttpContext, HealthReport, Task> responseWriter = (context, report) => CustomHealth.Writer(serviceName: "state", context, report);
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

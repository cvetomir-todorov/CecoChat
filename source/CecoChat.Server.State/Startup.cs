using Autofac;
using CecoChat.Autofac;
using CecoChat.Cassandra;
using CecoChat.Cassandra.Health;
using CecoChat.Contracts.Backplane;
using CecoChat.Data.State;
using CecoChat.Data.State.Telemetry;
using CecoChat.Jwt;
using CecoChat.Kafka;
using CecoChat.Kafka.Telemetry;
using CecoChat.Otel;
using CecoChat.Redis;
using CecoChat.Server.Backplane;
using CecoChat.Server.Config;
using CecoChat.Server.Health;
using CecoChat.Server.Identity;
using CecoChat.Server.State.Backplane;
using CecoChat.Server.State.Clients;
using CecoChat.Server.State.HostedServices;
using Confluent.Kafka;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.State;

public class Startup
{
    private readonly RedisOptions _configDbOptions;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly CassandraOptions _stateDbOptions;
    private readonly JwtOptions _jwtOptions;
    private readonly OtelSamplingOptions _otelSamplingOptions;
    private readonly JaegerOptions _jaegerOptions;
    private readonly PrometheusOptions _prometheusOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;

        _configDbOptions = new();
        configuration.GetSection("ConfigDB").Bind(_configDbOptions);

        _backplaneOptions = new();
        configuration.GetSection("Backplane").Bind(_backplaneOptions);

        _stateDbOptions = new();
        configuration.GetSection("StateDB").Bind(_stateDbOptions);

        _jwtOptions = new();
        configuration.GetSection("Jwt").Bind(_jwtOptions);

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
        AddTelemetryServices(services);
        AddHealthServices(services);

        // security
        services.AddJwtAuthentication(_jwtOptions);
        services.AddAuthorization();

        // clients
        services.AddGrpc(rpc => rpc.EnableDetailedErrors = Environment.IsDevelopment());

        // required
        services.AddOptions();
    }

    private void AddTelemetryServices(IServiceCollection services)
    {
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "State", serviceNamespace: "CecoChat", serviceVersion: "0.1")
            .AddEnvironmentVariableDetector();

        services.AddOpenTelemetryTracing(tracing =>
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
            tracing.ConfigureSampling(_otelSamplingOptions);
            tracing.ConfigureJaegerExporter(_jaegerOptions);
        });
        services.AddOpenTelemetryMetrics(metrics =>
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
                tags: new [] { HealthTags.Health, HealthTags.Startup })
            .AddCheck<ReceiversConsumerHealthCheck>(
                "receivers-consumer",
                tags: new[] { HealthTags.Health, HealthTags.Startup, HealthTags.Live })
            .AddCheck<SendersConsumerHealthCheck>(
                "senders-consumer",
                tags: new[] { HealthTags.Health, HealthTags.Startup, HealthTags.Live })
            .AddConfigDb(
                _configDbOptions,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddCassandra(
                name: "state-db",
                timeout: _stateDbOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddKafkaBackplane(
                _backplaneOptions.Kafka,
                _backplaneOptions.HealthProducer,
                _backplaneOptions.TopicHealth,
                timeout: _backplaneOptions.HealthTimeout,
                tags: new[] { HealthTags.Health });

        services.AddSingleton<StateDbInitHealthCheck>();
        services.AddSingleton<ReceiversConsumerHealthCheck>();
        services.AddSingleton<SendersConsumerHealthCheck>();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitStateDb>();
        builder.RegisterHostedService<StartBackplaneComponents>();

        // state db
        IConfiguration stateDbConfig = Configuration.GetSection("StateDB");
        builder.RegisterModule(new StateDbAutofacModule(stateDbConfig));
        builder.RegisterModule(new CassandraHealthAutofacModule(stateDbConfig));

        // backplane
        builder.RegisterType<StateConsumer>().As<IStateConsumer>().SingleInstance();
        builder.RegisterFactory<KafkaConsumer<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
        builder.RegisterModule(new KafkaAutofacModule());
        builder.RegisterOptions<BackplaneOptions>(Configuration.GetSection("Backplane"));
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
                setup.Health.ResponseWriter = (context, report) => CustomHealth.Writer(serviceName: "state", context, report);
            });
        });

        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == _prometheusOptions.ScrapeEndpointPath);
    }
}

using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.AspNet.Health;
using CecoChat.AspNet.Prometheus;
using CecoChat.Autofac;
using CecoChat.Client.IdGen;
using CecoChat.Contracts.Backplane;
using CecoChat.Data.Config;
using CecoChat.Data.User;
using CecoChat.Data.User.Infra;
using CecoChat.Http.Health;
using CecoChat.Kafka;
using CecoChat.Kafka.Health;
using CecoChat.Kafka.Telemetry;
using CecoChat.Npgsql.Health;
using CecoChat.Otel;
using CecoChat.Redis;
using CecoChat.Redis.Health;
using CecoChat.Server.Backplane;
using CecoChat.Server.Identity;
using CecoChat.Server.User.Backplane;
using CecoChat.Server.User.Endpoints;
using CecoChat.Server.User.HostedServices;
using CecoChat.Server.User.Security;
using Confluent.Kafka;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.User;

public class Startup : StartupBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly UserDbOptions _userDbOptions;
    private readonly RedisOptions _userCacheStoreOptions;
    private readonly IdGenOptions _idGenOptions;
    private readonly BackplaneOptions _backplaneOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        : base(configuration)
    {
        _environment = environment;

        _userDbOptions = new();
        Configuration.GetSection("UserDb").Bind(_userDbOptions);

        _userCacheStoreOptions = new();
        Configuration.GetSection("UserCache:Store").Bind(_userCacheStoreOptions);

        _idGenOptions = new();
        Configuration.GetSection("IdGen").Bind(_idGenOptions);

        _backplaneOptions = new();
        Configuration.GetSection("Backplane").Bind(_backplaneOptions);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        AddTelemetryServices(services);
        AddHealthServices(services);

        // security
        services.AddJwtAuthentication(JwtOptions);
        services.AddUserPolicyAuthorization();

        // clients
        services.AddGrpc(grpc =>
        {
            grpc.EnableDetailedErrors = _environment.IsDevelopment();
            grpc.EnableMessageValidation();
        });
        services.AddGrpcValidation();

        // user db
        services.AddUserDb(_userDbOptions.Connect);

        // id gen
        services.AddIdGenClient(_idGenOptions);

        // common
        services.AddAutoMapper(config =>
        {
            config.AddMaps(typeof(AutoMapperProfile));
        });
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddHttpContextAccessor();
        services.AddOptions();
    }

    private void AddTelemetryServices(IServiceCollection services)
    {
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "User", serviceNamespace: "CecoChat", serviceVersion: "0.1")
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
                        PrometheusOptions.ScrapeEndpointPath, HealthPaths.Health, HealthPaths.Startup, HealthPaths.Live, HealthPaths.Ready
                    };
                    aspnet.Filter = httpContext => !excludedPaths.Contains(httpContext.Request.Path);
                });
                tracing.AddKafkaInstrumentation();
                tracing.AddNpgsql();
                tracing.AddRedisInstrumentation();
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
        services
            .AddHealthChecks()
            .AddCheck<ConfigDbInitHealthCheck>(
                "config-db-init",
                tags: new[] { HealthTags.Health, HealthTags.Startup })
            .AddCheck<UserDbInitHealthCheck>(
                "user-db-init",
                tags: new[] { HealthTags.Health, HealthTags.Startup })
            .AddRedis(
                "config-db",
                ConfigDbOptions,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddKafka(
                "backplane",
                _backplaneOptions.Kafka,
                _backplaneOptions.Health,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddNpgsql(
                "user-db",
                _userDbOptions.Connect,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddRedis("user-cache",
                _userCacheStoreOptions,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddUri(
                "id-gen",
                new Uri(_idGenOptions.Address!, _idGenOptions.HealthPath),
                configureHttpClient: (_, client) => client.DefaultRequestVersion = new Version(2, 0),
                timeout: _idGenOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready });

        services.AddSingleton<ConfigDbInitHealthCheck>();
        services.AddSingleton<UserDbInitHealthCheck>();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitDynamicConfig>();
        builder.RegisterHostedService<InitBackplane>();
        builder.RegisterHostedService<InitBackplaneComponents>();
        builder.RegisterHostedService<InitUsersDb>();
        builder.RegisterHostedService<AsyncProfileCaching>();

        // config db
        IConfiguration configDbConfig = Configuration.GetSection("ConfigDb");
        builder.RegisterModule(new ConfigDbAutofacModule(configDbConfig, registerPartitioning: true));

        // backplane
        builder.RegisterType<KafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
        builder.RegisterOptions<KafkaOptions>(Configuration.GetSection("Backplane:Kafka"));
        builder.RegisterModule(new PartitionUtilityAutofacModule());
        builder.RegisterType<TopicPartitionFlyweight>().As<ITopicPartitionFlyweight>().SingleInstance();
        builder.RegisterFactory<KafkaProducer<Null, BackplaneMessage>, IKafkaProducer<Null, BackplaneMessage>>();
        builder.RegisterType<ConnectionNotifyProducer>().As<IConnectionNotifyProducer>().SingleInstance();
        builder.RegisterModule(new KafkaAutofacModule());
        builder.RegisterOptions<BackplaneOptions>(Configuration.GetSection("Backplane"));

        // user db
        IConfiguration userCacheConfig = Configuration.GetSection("UserCache");
        IConfiguration userCacheStoreConfig = userCacheConfig.GetSection("Store");
        builder.RegisterModule(new UserDbAutofacModule(userCacheConfig, userCacheStoreConfig));
        builder.RegisterOptions<UserDbOptions>(Configuration.GetSection("UserDb"));

        // id gen
        IConfiguration idGenConfiguration = Configuration.GetSection("IdGen");
        builder.RegisterModule(new IdGenAutofacModule(idGenConfiguration));

        // security
        builder.RegisterType<PasswordHasher>().As<IPasswordHasher>().SingleInstance();

        // shared
        builder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
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
            endpoints.MapGrpcService<AuthService>();
            endpoints.MapGrpcService<ProfileCommandService>();
            endpoints.MapGrpcService<ProfileQueryService>();
            endpoints.MapGrpcService<ConnectionCommandService>();
            endpoints.MapGrpcService<ConnectionQueryService>();

            endpoints.MapHttpHealthEndpoints(setup =>
            {
                Func<HttpContext, HealthReport, Task> responseWriter = (context, report) => CustomHealth.Writer(serviceName: "user", context, report);
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
    }
}

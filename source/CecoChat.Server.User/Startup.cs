using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.AspNet.Health;
using CecoChat.AspNet.Prometheus;
using CecoChat.Autofac;
using CecoChat.Client.Config;
using CecoChat.Contracts.Backplane;
using CecoChat.Data.User;
using CecoChat.Data.User.Infra;
using CecoChat.DynamicConfig;
using CecoChat.Kafka;
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
    private readonly UserDbOptions _userDbOptions;
    private readonly RedisOptions _userCacheStoreOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        : base(configuration, environment)
    {
        _userDbOptions = new();
        Configuration.GetSection("UserDb").Bind(_userDbOptions);

        _userCacheStoreOptions = new();
        Configuration.GetSection("UserCache:Store").Bind(_userCacheStoreOptions);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        AddTelemetryServices(services);
        AddHealthServices(services);

        // security
        services.AddJwtAuthentication(JwtOptions);
        services.AddUserPolicyAuthorization();

        // dynamic config
        services.AddConfigClient(ConfigClientOptions);

        // grpc
        services.AddGrpc(grpc =>
        {
            grpc.EnableDetailedErrors = Environment.IsDevelopment();
            grpc.EnableMessageValidation();
        });
        services.AddGrpcValidation();

        // user db
        services.AddUserDb(_userDbOptions.Connect);

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
                tracing
                    .SetResourceBuilder(serviceResourceBuilder)
                    .AddAspNetCoreServer(enableGrpcSupport: true, PrometheusOptions)
                    .AddKafkaInstrumentation()
                    .AddNpgsql()
                    .AddRedisInstrumentation()
                    .AddGrpcClientInstrumentation(grpc => grpc.SuppressDownstreamInstrumentation = true)
                    .ConfigureSampling(TracingSamplingOptions)
                    .ConfigureOtlpExporter(TracingExportOptions);
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(serviceResourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .ConfigurePrometheusAspNetExporter(PrometheusOptions);
            });
    }

    private void AddHealthServices(IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddDynamicConfigInit()
            .AddConfigChangesConsumer()
            .AddConfigService(ConfigClientOptions)
            .AddBackplane(Configuration)
            .AddCheck<UserDbInitHealthCheck>(
                "user-db-init",
                tags: new[] { HealthTags.Health, HealthTags.Startup })
            .AddNpgsql(
                "user-db",
                _userDbOptions.Connect,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddRedis(
                "user-cache",
                _userCacheStoreOptions,
                tags: new[] { HealthTags.Health, HealthTags.Ready });

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

        // dynamic config
        builder.RegisterModule(new DynamicConfigAutofacModule(
            Configuration.GetSection("Backplane"),
            registerConfigChangesConsumer: true,
            registerPartitioning: true));
        builder.RegisterModule(new ConfigClientAutofacModule(Configuration.GetSection("ConfigClient")));

        // backplane
        builder.RegisterType<KafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
        builder.RegisterOptions<KafkaOptions>(Configuration.GetSection("Backplane:Kafka"));
        builder.RegisterModule(new PartitionerAutofacModule());
        builder.RegisterType<TopicPartitionFlyweight>().As<ITopicPartitionFlyweight>().SingleInstance();
        builder.RegisterFactory<KafkaProducer<Null, BackplaneMessage>, IKafkaProducer<Null, BackplaneMessage>>();
        builder.RegisterType<ConnectionNotifyProducer>().As<IConnectionNotifyProducer>().SingleInstance();
        builder.RegisterModule(new KafkaAutofacModule());
        builder.RegisterOptions<BackplaneOptions>(Configuration.GetSection("Backplane"));

        // user db
        builder.RegisterModule(new UserDbAutofacModule(
            userCacheConfig: Configuration.GetSection("UserCache"),
            userCacheStoreConfig: Configuration.GetSection("UserCache:Store")));
        builder.RegisterOptions<UserDbOptions>(Configuration.GetSection("UserDb"));

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

        app.UseCustomExceptionHandler();
        app.UseHttpsRedirection();

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

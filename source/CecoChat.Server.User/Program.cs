using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.Client.Config;
using CecoChat.Contracts.Backplane;
using CecoChat.DynamicConfig;
using CecoChat.Server.Backplane;
using CecoChat.Server.Identity;
using CecoChat.Server.User.Backplane;
using CecoChat.Server.User.Endpoints.Auth;
using CecoChat.Server.User.Endpoints.Connections;
using CecoChat.Server.User.Endpoints.Files;
using CecoChat.Server.User.Endpoints.Profiles;
using CecoChat.Server.User.Init;
using CecoChat.Server.User.Security;
using CecoChat.User.Data;
using CecoChat.User.Data.Infra;
using Common;
using Common.AspNet.Health;
using Common.AspNet.Init;
using Common.AspNet.Prometheus;
using Common.Autofac;
using Common.Kafka;
using Common.Kafka.Telemetry;
using Common.Npgsql.Health;
using Common.OpenTelemetry;
using Common.Redis;
using Common.Redis.Health;
using Confluent.Kafka;
using FluentValidation;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.User;

public static class Program
{
    private static UserDbOptions _userDbOptions = null!;
    private static RedisOptions _userCacheStoreOptions = null!;

    public static async Task Main(params string[] args)
    {
        WebApplicationBuilder builder = EntryPoint.CreateWebAppBuilder(args);
        CommonOptions options = new(builder.Configuration);

        _userDbOptions = new();
        builder.Configuration.GetSection("UserDb").Bind(_userDbOptions);
        _userCacheStoreOptions = new();
        builder.Configuration.GetSection("UserCache:Store").Bind(_userCacheStoreOptions);

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
        // security
        builder.Services.AddJwtAuthentication(options.Jwt);
        builder.Services.AddUserPolicyAuthorization();

        // dynamic config
        builder.Services.AddConfigClient(options.ConfigClient);

        // grpc
        builder.Services.AddGrpc(grpc =>
        {
            grpc.EnableDetailedErrors = builder.Environment.IsDevelopment();
            grpc.EnableMessageValidation();
        });
        builder.Services.AddGrpcValidation();

        // user db
        builder.Services.AddUserDb(_userDbOptions.Connect);

        // common
        builder.Services.AddAutoMapper(config =>
        {
            config.AddMaps(typeof(AutoMapperProfile));
        });
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddOptions();
    }

    private static void AddTelemetry(WebApplicationBuilder builder, CommonOptions options)
    {
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "User", serviceNamespace: "CecoChat", serviceVersion: "0.1")
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
                    .AddRedisInstrumentation()
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
            .AddBackplane(builder.Configuration.GetSection("Backplane"))
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

        builder.Services.AddSingleton<UserDbInitHealthCheck>();
    }

    private static void ConfigureContainer(HostBuilderContext host, ContainerBuilder builder)
    {
        // init
        builder.RegisterInitStep<DynamicConfigInit>();
        builder.RegisterInitStep<BackplaneInit>();
        builder.RegisterInitStep<BackplaneComponentsInit>();
        builder.RegisterInitStep<UserDbInit>();
        builder.RegisterInitStep<AsyncProfileCachingInit>();

        // dynamic config
        builder.RegisterModule(new DynamicConfigAutofacModule(
            host.Configuration.GetSection("Backplane"),
            registerConfigChangesConsumer: true,
            registerPartitioning: true,
            registerUser: true));
        builder.RegisterModule(new ConfigClientAutofacModule(host.Configuration.GetSection("ConfigClient")));

        // backplane
        builder.RegisterType<KafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
        builder.RegisterOptions<KafkaOptions>(host.Configuration.GetSection("Backplane:Kafka"));
        builder.RegisterModule(new PartitionerAutofacModule());
        builder.RegisterType<TopicPartitionFlyweight>().As<ITopicPartitionFlyweight>().SingleInstance();
        builder.RegisterFactory<KafkaProducer<Null, BackplaneMessage>, IKafkaProducer<Null, BackplaneMessage>>();
        builder.RegisterType<ConnectionNotifyProducer>().As<IConnectionNotifyProducer>().SingleInstance();
        builder.RegisterModule(new KafkaAutofacModule());
        builder.RegisterOptions<BackplaneOptions>(host.Configuration.GetSection("Backplane"));

        // user db
        builder.RegisterModule(new UserDbAutofacModule(
            userCacheConfig: host.Configuration.GetSection("UserCache"),
            userCacheStoreConfig: host.Configuration.GetSection("UserCache:Store")));
        builder.RegisterOptions<UserDbOptions>(host.Configuration.GetSection("UserDb"));

        // security
        builder.RegisterType<PasswordHasher>().As<IPasswordHasher>().SingleInstance();

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
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGrpcService<AuthService>();
        app.MapGrpcService<ProfileCommandService>();
        app.MapGrpcService<ProfileQueryService>();
        app.MapGrpcService<ConnectionCommandService>();
        app.MapGrpcService<ConnectionQueryService>();
        app.MapGrpcService<FileCommandService>();
        app.MapGrpcService<FileQueryService>();
        app.MapCustomHttpHealthEndpoints(app.Environment, serviceName: "user");

        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == options.Prometheus.ScrapeEndpointPath);
    }
}

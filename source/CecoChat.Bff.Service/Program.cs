using System.Reflection;
using Autofac;
using CecoChat.Backplane;
using CecoChat.Bff.Service.Files;
using CecoChat.Bff.Service.Init;
using CecoChat.Chats.Client;
using CecoChat.Config;
using CecoChat.Config.Client;
using CecoChat.Data;
using CecoChat.Server;
using CecoChat.Server.Identity;
using CecoChat.User.Client;
using Common;
using Common.AspNet.FluentValidation;
using Common.AspNet.Health;
using Common.AspNet.Init;
using Common.AspNet.ModelBinding;
using Common.AspNet.Prometheus;
using Common.AspNet.Swagger;
using Common.Autofac;
using Common.Http.Health;
using Common.Jwt;
using Common.Kafka;
using Common.Kafka.Telemetry;
using Common.OpenTelemetry;
using Common.Seaweed;
using Common.Seaweed.Health;
using FluentValidation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Bff.Service;

public static class Program
{
    private static ChatsClientOptions _chatsClientOptions = null!;
    private static UserClientOptions _userClientOptions = null!;
    private static SeaweedOptions _seaweedOptions = null!;
    private static FilesOptions _filesOptions = null!;
    private static SwaggerOptions _swaggerOptions = null!;

    public static async Task<int> Main(params string[] args)
    {
        return await EntryPoint.Run(args, typeof(Program), ConfigureBuilder, ConfigurePipeline);
    }

    private static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        CommonOptions commonOptions = new(builder.Configuration);

        _chatsClientOptions = new();
        builder.Configuration.GetSection("ChatsClient").Bind(_chatsClientOptions);
        _userClientOptions = new();
        builder.Configuration.GetSection("UserClient").Bind(_userClientOptions);
        _seaweedOptions = new();
        builder.Configuration.GetSection("FileStorage").Bind(_seaweedOptions);
        _filesOptions = new();
        builder.Configuration.GetSection("Files").Bind(_filesOptions);
        _swaggerOptions = new();
        builder.Configuration.GetSection("Swagger").Bind(_swaggerOptions);

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.Limits.MaxRequestBodySize = _filesOptions.MaxRequestBodyBytes;
        });

        AddServices(builder, commonOptions);
        AddTelemetry(builder, commonOptions);
        AddHealth(builder, commonOptions);

        builder.Host.ConfigureContainer<ContainerBuilder>(ConfigureContainer);
    }

    private static void AddServices(WebApplicationBuilder builder, CommonOptions commonOptions)
    {
        // security
        builder.Services.AddJwtAuthentication(commonOptions.Jwt);
        builder.Services.AddUserPolicyAuthorization();

        // dynamic config
        builder.Services.AddConfigClient(commonOptions.ConfigClient);

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

        // downstream services
        builder.Services.AddChatsClient(_chatsClientOptions);
        builder.Services.AddUserClient(_userClientOptions);

        // common
        builder.Services.AddAutoMapper(config =>
        {
            config.AddMaps(typeof(AutoMapperProfile));
        });
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddOptions();
        builder.Services.AddSingleton(commonOptions);
    }

    private static void AddTelemetry(WebApplicationBuilder builder, CommonOptions options)
    {
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "Bff", serviceNamespace: "CecoChat", serviceVersion: "0.1")
            .AddEnvironmentVariableDetector();

        builder.Services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
            {
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
            .AddBackplane(builder.Configuration.GetSection("Backplane"))
            .AddCheck<FileStorageInitHealthCheck>(
                "file-storage-init",
                tags: [HealthTags.Health, HealthTags.Startup])
            .AddUri(
                "chats-svc",
                new Uri(_chatsClientOptions.Address!, _chatsClientOptions.HealthPath),
                configureHttpClient: (_, client) => client.DefaultRequestVersion = new Version(2, 0),
                timeout: _chatsClientOptions.HealthTimeout,
                tags: [HealthTags.Health, HealthTags.Ready])
            .AddUri(
                "user-svc",
                new Uri(_userClientOptions.Address!, _userClientOptions.HealthPath),
                configureHttpClient: (_, client) => client.DefaultRequestVersion = new Version(2, 0),
                timeout: _userClientOptions.HealthTimeout,
                tags: [HealthTags.Health, HealthTags.Ready])
            .AddSeaweed(
                "file-storage",
                _seaweedOptions,
                tags: [HealthTags.Health, HealthTags.Ready]);

        builder.Services.AddSingleton<FileStorageInitHealthCheck>();
    }

    private static void ConfigureContainer(HostBuilderContext host, ContainerBuilder builder)
    {
        // init
        builder.RegisterInitStep<DynamicConfigInit>();
        builder.RegisterInitStep<BackplaneInit>();
        builder.RegisterInitStep<BackplaneComponentsInit>();
        builder.RegisterInitStep<FileStorageInit>();

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

        // downstream services
        builder.RegisterModule(new ChatsClientAutofacModule(host.Configuration.GetSection("ChatsClient")));
        builder.RegisterModule(new UserClientAutofacModule(host.Configuration.GetSection("UserClient")));

        // files
        builder.RegisterModule(new SeaweedAutofacModule(host.Configuration.GetSection("FileStorage")));
        builder.RegisterType<ObjectNaming>().As<IObjectNaming>().SingleInstance();
        builder.RegisterType<FileUtility>().As<IFileUtility>().SingleInstance();
        builder.RegisterOptions<FilesOptions>(host.Configuration.GetSection("Files"));

        // security
        builder.RegisterOptions<JwtOptions>(host.Configuration.GetSection("Jwt"));

        // shared
        builder.RegisterType<ContractMapper>().As<IContractMapper>().SingleInstance();
        builder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
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

        app.MapControllers();
        app.MapCustomHttpHealthEndpoints(app.Environment, serviceName: "bff");

        CommonOptions commonOptions = app.Services.GetRequiredService<CommonOptions>();
        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == commonOptions.Prometheus.ScrapeEndpointPath);
        app.MapWhen(context => context.Request.Path.StartsWithSegments("/swagger"), _ =>
        {
            app.UseSwaggerMiddlewares(_swaggerOptions);
        });
    }
}

using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.Chats.Data;
using CecoChat.Chats.Data.Telemetry;
using CecoChat.Client.Config;
using CecoChat.Contracts.Backplane;
using CecoChat.DynamicConfig;
using CecoChat.Server.Backplane;
using CecoChat.Server.Chats.Backplane;
using CecoChat.Server.Chats.Endpoints;
using CecoChat.Server.Chats.Init;
using CecoChat.Server.Identity;
using Common;
using Common.AspNet.Health;
using Common.AspNet.Init;
using Common.AspNet.Prometheus;
using Common.Autofac;
using Common.Cassandra;
using Common.Cassandra.Health;
using Common.Kafka;
using Common.Kafka.Telemetry;
using Common.OpenTelemetry;
using Confluent.Kafka;
using FluentValidation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.Chats;

public static class Program
{
    private static CassandraOptions _chatsDbOptions = null!;

    public static async Task Main(params string[] args)
    {
        WebApplicationBuilder builder = EntryPoint.CreateWebAppBuilder(args);
        CommonOptions options = new(builder.Configuration);

        _chatsDbOptions = new();
        builder.Configuration.GetSection("ChatsDb").Bind(_chatsDbOptions);

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

        // common
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddOptions();
    }

    private static void AddTelemetry(WebApplicationBuilder builder, CommonOptions options)
    {
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "Chats", serviceNamespace: "CecoChat", serviceVersion: "0.1")
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
                    .AddChatsInstrumentation()
                    .ConfigureSampling(options.TracingSampling)
                    .ConfigureOtlpExporter(options.TracingExport);
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(serviceResourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddChatsInstrumentation()
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
            .AddCheck<ChatsDbInitHealthCheck>(
                "chats-db-init",
                tags: new[] { HealthTags.Health, HealthTags.Startup })
            .AddCassandra(
                name: "chats-db",
                timeout: _chatsDbOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddCheck<HistoryConsumerHealthCheck>(
                "history-consumer",
                tags: new[] { HealthTags.Health, HealthTags.Startup, HealthTags.Live })
            .AddCheck<ReceiversConsumerHealthCheck>(
                "receivers-consumer",
                tags: new[] { HealthTags.Health, HealthTags.Startup, HealthTags.Live })
            .AddCheck<SendersConsumerHealthCheck>(
                "senders-consumer",
                tags: new[] { HealthTags.Health, HealthTags.Startup, HealthTags.Live });

        builder.Services.AddSingleton<ChatsDbInitHealthCheck>();
        builder.Services.AddSingleton<HistoryConsumerHealthCheck>();
        builder.Services.AddSingleton<ReceiversConsumerHealthCheck>();
        builder.Services.AddSingleton<SendersConsumerHealthCheck>();
    }

    private static void ConfigureContainer(HostBuilderContext host, ContainerBuilder builder)
    {
        // init
        builder.RegisterInitStep<DynamicConfigInit>();
        builder.RegisterInitStep<ChatsDbInit>();
        builder.RegisterInitStep<BackplaneInit>();
        builder.RegisterInitStep<BackplaneComponentsInit>();

        // dynamic config
        builder.RegisterModule(new DynamicConfigAutofacModule(
            host.Configuration.GetSection("Backplane"),
            registerConfigChangesConsumer: true,
            registerHistory: true));
        builder.RegisterModule(new ConfigClientAutofacModule(host.Configuration.GetSection("ConfigClient")));

        // chats db
        builder.RegisterModule(new ChatsDbAutofacModule(host.Configuration.GetSection("ChatsDb")));
        builder.RegisterModule(new CassandraHealthAutofacModule(host.Configuration.GetSection("ChatsDb")));

        // backplane
        builder.RegisterType<KafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
        builder.RegisterOptions<KafkaOptions>(host.Configuration.GetSection("Backplane:Kafka"));
        builder.RegisterType<HistoryConsumer>().As<IHistoryConsumer>().SingleInstance();
        builder.RegisterType<StateConsumer>().As<IStateConsumer>().SingleInstance();
        builder.RegisterFactory<KafkaConsumer<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
        builder.RegisterModule(new KafkaAutofacModule());
        builder.RegisterOptions<BackplaneOptions>(host.Configuration.GetSection("Backplane"));

        // shared
        builder.RegisterType<ContractMapper>().As<IContractMapper>().SingleInstance();
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

        app.MapGrpcService<ChatsService>();
        app.MapCustomHttpHealthEndpoints(app.Environment, serviceName: "chats");

        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == options.Prometheus.ScrapeEndpointPath);
    }
}

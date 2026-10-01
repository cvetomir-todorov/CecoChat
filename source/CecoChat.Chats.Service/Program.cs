using System.Reflection;
using Autofac;
using Calzolari.Grpc.AspNetCore.Validation;
using CecoChat.Backplane;
using CecoChat.Backplane.Contracts;
using CecoChat.Chats.Data;
using CecoChat.Chats.Data.Telemetry;
using CecoChat.Chats.Service.Backplane;
using CecoChat.Chats.Service.Endpoints;
using CecoChat.Chats.Service.Init;
using CecoChat.Config;
using CecoChat.Config.Client;
using CecoChat.Server;
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

namespace CecoChat.Chats.Service;

public static class Program
{
    public static async Task<int> Main(params string[] args)
    {
        return await EntryPoint.Run(args, typeof(Program), ConfigureBuilder, ConfigurePipeline);
    }

    private static void ConfigureBuilder(WebApplicationBuilder builder)
    {
        CommonOptions commonOptions = new(builder.Configuration);

        CassandraOptions chatsDbOptions = new();
        builder.Configuration.GetSection("ChatsDb:Cluster").Bind(chatsDbOptions);

        AddServices(builder, commonOptions);
        AddTelemetry(builder, commonOptions);
        AddHealth(builder, commonOptions, chatsDbOptions);

        builder.Host.ConfigureContainer<ContainerBuilder>(ConfigureContainer);
    }

    public static void AddServices(WebApplicationBuilder builder, CommonOptions commonOptions)
    {
        // security
        builder.Services.AddJwtAuthentication(commonOptions.Jwt);
        builder.Services.AddUserPolicyAuthorization();

        // dynamic config
        builder.Services.AddConfigClient(commonOptions.ConfigClient);

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
        builder.Services.AddSingleton(commonOptions);
    }

    public static void AddTelemetry(WebApplicationBuilder builder, CommonOptions options)
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

    public static void AddHealth(WebApplicationBuilder builder, CommonOptions options, CassandraOptions chatsDbOptions)
    {
        builder.Services
            .AddHealthChecks()
            .AddDynamicConfigInit()
            .AddConfigChangesConsumer()
            .AddConfigService(options.ConfigClient)
            .AddBackplane(builder.Configuration.GetSection("Backplane"))
            .AddCheck<ChatsDbInitHealthCheck>(
                "chats-db-init",
                tags: [HealthTags.Health, HealthTags.Startup])
            .AddCassandra<IChatsDbContext>(
                name: "chats-db",
                timeout: chatsDbOptions.HealthTimeout,
                tags: [HealthTags.Health, HealthTags.Ready])
            .AddCheck<HistoryConsumerHealthCheck>(
                "history-consumer",
                tags: [HealthTags.Health, HealthTags.Startup, HealthTags.Live])
            .AddCheck<ReceiversConsumerHealthCheck>(
                "receivers-consumer",
                tags: [HealthTags.Health, HealthTags.Startup, HealthTags.Live])
            .AddCheck<SendersConsumerHealthCheck>(
                "senders-consumer",
                tags: [HealthTags.Health, HealthTags.Startup, HealthTags.Live]);

        builder.Services.AddSingleton<ChatsDbInitHealthCheck>();
        builder.Services.AddSingleton<HistoryConsumerHealthCheck>();
        builder.Services.AddSingleton<ReceiversConsumerHealthCheck>();
        builder.Services.AddSingleton<SendersConsumerHealthCheck>();
    }

    public static void ConfigureContainer(HostBuilderContext host, ContainerBuilder builder)
    {
        // init
        builder.RegisterInit();
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
        builder.RegisterModule(new ChatsDbAutofacModule(
            clusterConfiguration: host.Configuration.GetSection("ChatsDb:Cluster"),
            chatMessagesOperationsConfiguration: host.Configuration.GetSection("ChatsDb:Operations:ChatMessages"),
            userChatsOperationsConfiguration: host.Configuration.GetSection("ChatsDb:Operations:UserChats")));

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

    public static void ConfigurePipeline(WebApplication app)
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

        CommonOptions commonOptions = app.Services.GetRequiredService<CommonOptions>();
        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == commonOptions.Prometheus.ScrapeEndpointPath);
    }
}

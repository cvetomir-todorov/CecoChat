using Autofac;
using CecoChat.Config;
using CecoChat.Config.Client;
using CecoChat.Contracts.Backplane;
using CecoChat.IdGen.Client;
using CecoChat.Messaging.Service.Backplane;
using CecoChat.Messaging.Service.Clients;
using CecoChat.Messaging.Service.Endpoints;
using CecoChat.Messaging.Service.Init;
using CecoChat.Messaging.Service.Telemetry;
using CecoChat.Server;
using CecoChat.Server.Backplane;
using CecoChat.Server.Identity;
using Common;
using Common.AspNet.Health;
using Common.AspNet.Init;
using Common.AspNet.Prometheus;
using Common.AspNet.SignalR.Telemetry;
using Common.Autofac;
using Common.Http.Health;
using Common.Kafka;
using Common.Kafka.Telemetry;
using Common.OpenTelemetry;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Messaging.Service;

public static class Program
{
    private static ClientOptions _clientOptions = null!;
    private static IdGenClientOptions _idGenClientOptions = null!;

    public static async Task Main(params string[] args)
    {
        WebApplicationBuilder builder = EntryPoint.CreateWebAppBuilder(args);
        CommonOptions options = new(builder.Configuration);

        _clientOptions = new();
        builder.Configuration.GetSection("Clients").Bind(_clientOptions);
        _idGenClientOptions = new();
        builder.Configuration.GetSection("IdGenClient").Bind(_idGenClientOptions);

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

        // signalr
        builder.Services
            .AddSignalR(signalr =>
            {
                signalr.EnableDetailedErrors = builder.Environment.IsDevelopment();
                // when clients don't send anything within this interval, server disconnects them in order to save resources
                signalr.ClientTimeoutInterval = _clientOptions.TimeoutInterval;
                // the server sends data to keep the connection alive
                signalr.KeepAliveInterval = _clientOptions.KeepAliveInterval;
            })
            .AddMessagePackProtocol()
            .AddHubOptions<ChatHub>(chatHub =>
            {
                chatHub.AddFilter<SignalRTelemetryFilter>();
            });

        // idgen
        builder.Services.AddIdGenClient(_idGenClientOptions);

        // common
        builder.Services.AddOptions();
    }

    private static void AddTelemetry(WebApplicationBuilder builder, CommonOptions options)
    {
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "Messaging", serviceNamespace: "CecoChat", serviceVersion: "0.1")
            .AddEnvironmentVariableDetector();

        builder.Services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(serviceResourceBuilder)
                    .AddSignalRInstrumentation()
                    .AddKafkaInstrumentation()
                    .AddGrpcClientInstrumentation(grpc => grpc.SuppressDownstreamInstrumentation = true)
                    .ConfigureSampling(options.TracingSampling)
                    .ConfigureOtlpExporter(options.TracingExport);
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(serviceResourceBuilder)
                    .AddSignalRInstrumentation()
                    .AddMessagingInstrumentation()
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
            .AddCheck<ReceiversConsumerHealthCheck>(
                "receivers-consumer",
                tags: new[] { HealthTags.Health, HealthTags.Startup, HealthTags.Live })
            .AddUri(
                "idgen-svc",
                new Uri(_idGenClientOptions.Address!, _idGenClientOptions.HealthPath),
                configureHttpClient: (_, client) => client.DefaultRequestVersion = new Version(2, 0),
                timeout: _idGenClientOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready });

        builder.Services.AddSingleton<ReceiversConsumerHealthCheck>();
    }

    private static void ConfigureContainer(HostBuilderContext host, ContainerBuilder builder)
    {
        // init
        builder.RegisterInitStep<DynamicConfigInit>();
        builder.RegisterInitStep<BackplaneInit>();
        builder.RegisterInitStep<BackplaneComponentsInit>();

        // config
        builder.RegisterOptions<ConfigOptions>(host.Configuration.GetSection("Config"));

        // dynamic config
        builder.RegisterModule(new DynamicConfigAutofacModule(
            host.Configuration.GetSection("Backplane"),
            registerConfigChangesConsumer: true,
            registerPartitioning: true));
        builder.RegisterModule(new ConfigClientAutofacModule(host.Configuration.GetSection("ConfigClient")));

        // clients
        builder.RegisterType<ClientContainer>().As<IClientContainer>().SingleInstance();
        builder.RegisterType<InputValidator>().As<IInputValidator>().SingleInstance();
        builder.RegisterModule(new SignalRTelemetryAutofacModule());

        // idgen
        builder.RegisterModule(new IdGenClientAutofacModule(host.Configuration.GetSection("IdGenClient")));

        // backplane
        builder.RegisterType<KafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
        builder.RegisterOptions<KafkaOptions>(host.Configuration.GetSection("Backplane:Kafka"));
        builder.RegisterModule(new PartitionerAutofacModule());
        builder.RegisterType<BackplaneComponents>().As<IBackplaneComponents>().SingleInstance();
        builder.RegisterType<TopicPartitionFlyweight>().As<ITopicPartitionFlyweight>().SingleInstance();
        builder.RegisterType<SendersProducer>().As<ISendersProducer>().SingleInstance();
        builder.RegisterType<ReceiversConsumer>().As<IReceiversConsumer>().SingleInstance();
        builder.RegisterFactory<KafkaProducer<Null, BackplaneMessage>, IKafkaProducer<Null, BackplaneMessage>>();
        builder.RegisterFactory<KafkaConsumer<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
        builder.RegisterModule(new KafkaAutofacModule());
        builder.RegisterOptions<BackplaneOptions>(host.Configuration.GetSection("Backplane"));

        // shared
        builder.RegisterType<MessagingTelemetry>().As<IMessagingTelemetry>().SingleInstance();
        builder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
        builder.RegisterType<ContractMapper>().As<IContractMapper>().SingleInstance();
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

        app.MapHub<ChatHub>("/chat");
        app.MapCustomHttpHealthEndpoints(app.Environment, serviceName: "messaging");

        app.UseOpenTelemetryPrometheusScrapingEndpoint(context => context.Request.Path == options.Prometheus.ScrapeEndpointPath);
    }
}

using Autofac;
using CecoChat.AspNet.Health;
using CecoChat.AspNet.Prometheus;
using CecoChat.AspNet.SignalR.Telemetry;
using CecoChat.Autofac;
using CecoChat.Client.IdGen;
using CecoChat.Contracts.Backplane;
using CecoChat.Data.Config;
using CecoChat.Http.Health;
using CecoChat.Kafka;
using CecoChat.Kafka.Health;
using CecoChat.Kafka.Telemetry;
using CecoChat.Otel;
using CecoChat.Redis.Health;
using CecoChat.Server.Backplane;
using CecoChat.Server.Identity;
using CecoChat.Server.Messaging.Backplane;
using CecoChat.Server.Messaging.Clients;
using CecoChat.Server.Messaging.Endpoints;
using CecoChat.Server.Messaging.HostedServices;
using CecoChat.Server.Messaging.Telemetry;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CecoChat.Server.Messaging;

public class Startup : StartupBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ClientOptions _clientOptions;
    private readonly BackplaneOptions _backplaneOptions;
    private readonly IdGenClientOptions _idGenClientOptions;

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        : base(configuration)
    {
        _environment = environment;

        _clientOptions = new();
        Configuration.GetSection("Clients").Bind(_clientOptions);

        _backplaneOptions = new();
        Configuration.GetSection("Backplane").Bind(_backplaneOptions);

        _idGenClientOptions = new();
        Configuration.GetSection("IdGenClient").Bind(_idGenClientOptions);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        AddTelemetryServices(services);
        AddHealthServices(services);

        // security
        services.AddJwtAuthentication(JwtOptions);
        services.AddUserPolicyAuthorization();

        // idgen
        services.AddIdGenClient(_idGenClientOptions);

        // clients
        services
            .AddSignalR(signalr =>
            {
                signalr.EnableDetailedErrors = _environment.IsDevelopment();
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

        // common
        services.AddOptions();
    }

    private void AddTelemetryServices(IServiceCollection services)
    {
        ResourceBuilder serviceResourceBuilder = ResourceBuilder
            .CreateEmpty()
            .AddService(serviceName: "Messaging", serviceNamespace: "CecoChat", serviceVersion: "0.1")
            .AddEnvironmentVariableDetector();

        services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(serviceResourceBuilder);
                tracing.AddSignalRInstrumentation();
                tracing.AddKafkaInstrumentation();
                tracing.ConfigureSampling(TracingSamplingOptions);
                tracing.ConfigureOtlpExporter(TracingExportOptions);
            })
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(serviceResourceBuilder);
                metrics.AddSignalRInstrumentation();
                metrics.AddMessagingInstrumentation();
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
            .AddCheck<ReceiversConsumerHealthCheck>(
                "receivers-consumer",
                tags: new[] { HealthTags.Health, HealthTags.Startup, HealthTags.Live })
            .AddRedis(
                "config-db",
                ConfigDbOptions,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddKafka(
                "backplane",
                _backplaneOptions.Kafka,
                _backplaneOptions.Health,
                tags: new[] { HealthTags.Health, HealthTags.Ready })
            .AddUri(
                "idgen",
                new Uri(_idGenClientOptions.Address!, _idGenClientOptions.HealthPath),
                configureHttpClient: (_, client) => client.DefaultRequestVersion = new Version(2, 0),
                timeout: _idGenClientOptions.HealthTimeout,
                tags: new[] { HealthTags.Health, HealthTags.Ready });

        services.AddSingleton<ConfigDbInitHealthCheck>();
        services.AddSingleton<ReceiversConsumerHealthCheck>();
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
        // ordered hosted services
        builder.RegisterHostedService<InitDynamicConfig>();
        builder.RegisterHostedService<InitBackplane>();
        builder.RegisterHostedService<InitBackplaneComponents>();

        // configuration
        IConfiguration configDbConfig = Configuration.GetSection("ConfigDb");
        builder.RegisterModule(new ConfigDbAutofacModule(configDbConfig, registerPartitioning: true));
        builder.RegisterOptions<ConfigOptions>(Configuration.GetSection("Config"));

        // clients
        builder.RegisterType<ClientContainer>().As<IClientContainer>().SingleInstance();
        builder.RegisterType<InputValidator>().As<IInputValidator>().SingleInstance();
        builder.RegisterModule(new SignalRTelemetryAutofacModule());

        // idgen
        IConfiguration idGenConfiguration = Configuration.GetSection("IdGenClient");
        builder.RegisterModule(new IdGenClientAutofacModule(idGenConfiguration));

        // backplane
        builder.RegisterType<KafkaAdmin>().As<IKafkaAdmin>().SingleInstance();
        builder.RegisterOptions<KafkaOptions>(Configuration.GetSection("Backplane:Kafka"));
        builder.RegisterModule(new PartitionerAutofacModule());
        builder.RegisterType<BackplaneComponents>().As<IBackplaneComponents>().SingleInstance();
        builder.RegisterType<TopicPartitionFlyweight>().As<ITopicPartitionFlyweight>().SingleInstance();
        builder.RegisterType<SendersProducer>().As<ISendersProducer>().SingleInstance();
        builder.RegisterType<ReceiversConsumer>().As<IReceiversConsumer>().SingleInstance();
        builder.RegisterFactory<KafkaProducer<Null, BackplaneMessage>, IKafkaProducer<Null, BackplaneMessage>>();
        builder.RegisterFactory<KafkaConsumer<Null, BackplaneMessage>, IKafkaConsumer<Null, BackplaneMessage>>();
        builder.RegisterModule(new KafkaAutofacModule());
        builder.RegisterOptions<BackplaneOptions>(Configuration.GetSection("Backplane"));

        // shared
        builder.RegisterType<MessagingTelemetry>().As<IMessagingTelemetry>().SingleInstance();
        builder.RegisterType<MonotonicClock>().As<IClock>().SingleInstance();
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
            endpoints.MapHub<ChatHub>("/chat");

            endpoints.MapHttpHealthEndpoints(setup =>
            {
                Func<HttpContext, HealthReport, Task> responseWriter = (context, report) => CustomHealth.Writer(serviceName: "messaging", context, report);
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
